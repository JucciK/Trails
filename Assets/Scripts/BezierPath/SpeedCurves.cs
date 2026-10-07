using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using System.IO;

public class SpeedCurves : MonoBehaviour
{

    public rMap rMap;
    public float[] d_speeds = new float[] { 0.5f, 1.0f, 1.5f };
    public float[] d_accelerations = new float[] { 0.25f, 1.0f, 2.0f };
    public float total_length = 100;
    public float avg_speed = 1.0f;
    public int segment_count = 2;
    public float max_radians_per_second = 3.14159f;
    public float max_ang_acc = 15f;
    public float default_acceleration = 0.5f;



    public bool generateFiles(string file, float totalTime)
    {
        const float dt = 0.1f;

        string dir = System.IO.Path.GetDirectoryName(file);
        string speedPath = System.IO.Path.Combine(dir, "speeds");
        string positionPath = System.IO.Path.Combine(dir, "positions");
        string filename = System.IO.Path.GetFileName(file);

        System.IO.Directory.CreateDirectory(speedPath);
        System.IO.Directory.CreateDirectory(positionPath);

        // ------------------------------------------------------------
        // Load Bezier path
        // ------------------------------------------------------------

        List<Bezier> beziers = new List<Bezier>();

        using (StreamReader sr = new StreamReader(file))
        {
            string line;

            while ((line = sr.ReadLine()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    beziers.Add(new Bezier(line));
            }
        }

        if (beziers.Count == 0)
        {
            Debug.LogError($"No Bezier curves found in {file}");
            return false;
        }

        // ------------------------------------------------------------
        // Generate speed profile
        // ------------------------------------------------------------

        List<SpeedSegment> segments = generateSpeedCurve(beziers);

        if (segments == null || segments.Count == 0)
        {
            Debug.LogError("Speed curve generation failed.");
            return false;
        }

        float profileTime = 0f;

        foreach (SpeedSegment s in segments)
            profileTime += s.time;

        Debug.Log(
            $"Speed profile: {segments.Count} segments, " +
            $"time={profileTime:F4}s, requested={totalTime:F4}s");

        if (Mathf.Abs(profileTime - totalTime) > 0.01f)
        {
            Debug.LogError(
                $"Speed profile has incorrect duration. " +
                $"Expected {totalTime:F4}s, got {profileTime:F4}s");

            return false;
        }

        // Number of OUTPUT samples.
        //
        // totalTime = 900:
        // t = 0.0 ... 899.9
        // => 9000 samples
        int n = Mathf.RoundToInt(totalTime / dt);

        List<float> distances = new List<float>(n);
        List<float> speeds = new List<float>(n);
        List<float> accels = new List<float>(n);
        List<float> kappas = new List<float>(n);
        List<float> ang_vel = new List<float>(n);
        List<float> ang_acc = new List<float>(n);

        List<Vector3> positions = new List<Vector3>(n);
        List<Vector3> tans = new List<Vector3>(n);

        // ------------------------------------------------------------
        // State for walking through the speed profile
        // ------------------------------------------------------------

        int segmentIndex = 0;
        float segmentTime = 0f;
        float distance = 0f;

        // ------------------------------------------------------------
        // Sample trajectory
        // ------------------------------------------------------------

        for (int i = 0; i < n; i++)
        {
            float globalTime = i * dt;

            if (segmentIndex >= segments.Count)
            {
                Debug.LogError(
                    $"Ran out of speed segments at " +
                    $"sample {i}, t={globalTime:F3}");

                return false;
            }

            SpeedSegment segment = segments[segmentIndex];

            // --------------------------------------------------------
            // Values AT the current time
            // --------------------------------------------------------

            float speed = segment.calculate_speed(segmentTime);
            float acceleration = segment.get_acceleration();

            if (!IsFinite(speed) || !IsFinite(acceleration))
            {
                Debug.LogError(
                    $"Invalid speed profile value at t={globalTime:F3}: " +
                    $"segment={segmentIndex}, " +
                    $"localTime={segmentTime}, " +
                    $"speed={speed}, acceleration={acceleration}");

                return false;
            }

            // --------------------------------------------------------
            // Find position on Bezier path
            // --------------------------------------------------------

            Bezier bez;
            float bezT;

            (bez, bezT) = get_t_by_distance(beziers, distance);

            if (!IsFinite(bezT))
            {
                Debug.LogError(
                    $"Invalid Bezier t at sample {i}: " +
                    $"time={globalTime:F3}, distance={distance}, t={bezT}");

                return false;
            }

            if (bezT < -0.001f || bezT > 1.001f)
            {
                Debug.LogError(
                    $"Bezier t outside [0,1]: " +
                    $"time={globalTime:F3}, distance={distance}, t={bezT}");

                return false;
            }

            float kappa = bez.get_curvature(bezT);

            if (!IsFinite(kappa))
            {
                Debug.LogError(
                    $"Invalid curvature at sample {i}: " +
                    $"time={globalTime:F3}, " +
                    $"distance={distance:F4}, " +
                    $"Bezier t={bezT:R}, " +
                    $"speed={speed:F4}");

                return false;
            }

            (Vector3 position, Vector3 tangent) =
                get_position_by_distance(beziers, distance);

            if (!IsFinite(position) || !IsFinite(tangent))
            {
                Debug.LogError(
                    $"Invalid position/tangent at sample {i}: " +
                    $"time={globalTime:F3}, distance={distance:F4}");

                return false;
            }

            // --------------------------------------------------------
            // Store current sample
            // --------------------------------------------------------

            distances.Add(distance);
            speeds.Add(speed);
            accels.Add(acceleration);
            kappas.Add(kappa);

            positions.Add(position);
            tans.Add(tangent);

            // omega = kappa * v
            float omega = kappa * speed;

            if (!IsFinite(omega))
            {
                Debug.LogError(
                    $"Invalid angular velocity at sample {i}: " +
                    $"kappa={kappa}, speed={speed}");

                return false;
            }

            ang_vel.Add(omega);

            // --------------------------------------------------------
            // Advance exactly dt seconds through speed profile.
            //
            // The while loop makes this correct even if dt crosses
            // one or more SpeedSegment boundaries.
            // --------------------------------------------------------

            float remainingDt = dt;

            while (remainingDt > 1e-7f)
            {
                if (segmentIndex >= segments.Count)
                {
                    // This is okay only after the final output sample.
                    if (i == n - 1)
                        break;

                    Debug.LogError(
                        $"Speed profile ended early at " +
                        $"t={globalTime:F3}");

                    return false;
                }

                segment = segments[segmentIndex];

                float remainingSegmentTime =
                    segment.time - segmentTime;

                // Protect against zero-duration segments.
                if (remainingSegmentTime <= 1e-7f)
                {
                    segmentIndex++;
                    segmentTime = 0f;
                    continue;
                }

                float step =
                    Mathf.Min(remainingDt, remainingSegmentTime);

                // Integrate exact distance for this part of the
                // SpeedSegment.
                float d = segment.calculate_distance(
                    segmentTime,
                    segmentTime + step);

                if (!IsFinite(d))
                {
                    Debug.LogError(
                        $"Invalid distance increment: " +
                        $"segment={segmentIndex}, " +
                        $"localTime={segmentTime}, " +
                        $"step={step}, d={d}");

                    return false;
                }

                distance += d;
                segmentTime += step;
                remainingDt -= step;

                // Move to next segment when this one is finished.
                if (segmentTime >= segment.time - 1e-6f)
                {
                    segmentIndex++;
                    segmentTime = 0f;
                }
            }
        }

        // ------------------------------------------------------------
        // Angular acceleration
        //
        // omega = kappa * v
        // alpha = d(omega)/dt
        //
        // Numerically differentiating omega directly avoids having
        // two separate numerical derivatives that can disagree.
        // ------------------------------------------------------------

        for (int i = 0; i < n; i++)
        {
            float alpha;

            if (n == 1)
            {
                alpha = 0f;
            }
            else if (i == 0)
            {
                // Forward difference
                alpha =
                    (ang_vel[i + 1] - ang_vel[i]) / dt;
            }
            else if (i == n - 1)
            {
                // Backward difference
                alpha =
                    (ang_vel[i] - ang_vel[i - 1]) / dt;
            }
            else
            {
                // Central difference
                alpha =
                    (ang_vel[i + 1] - ang_vel[i - 1])
                    / (2f * dt);
            }

            if (!IsFinite(alpha))
            {
                Debug.LogError(
                    $"Invalid angular acceleration at sample {i}: " +
                    $"omega={ang_vel[i]}");

                return false;
            }

            ang_acc.Add(alpha);
        }

        // ------------------------------------------------------------
        // Final sanity check
        // ------------------------------------------------------------

        for (int i = 0; i < n; i++)
        {
            if (!IsFinite(speeds[i]) ||
                !IsFinite(accels[i]) ||
                !IsFinite(distances[i]) ||
                !IsFinite(ang_vel[i]) ||
                !IsFinite(ang_acc[i]) ||
                !IsFinite(kappas[i]))
            {
                Debug.LogError(
                    $"Invalid output at sample {i}:\n" +
                    $"speed={speeds[i]}\n" +
                    $"acceleration={accels[i]}\n" +
                    $"distance={distances[i]}\n" +
                    $"angular velocity={ang_vel[i]}\n" +
                    $"angular acceleration={ang_acc[i]}\n" +
                    $"kappa={kappas[i]}");

                return false;
            }
        }

        // ------------------------------------------------------------
        // Write files
        // ------------------------------------------------------------

        string speedFile =
            System.IO.Path.Combine(speedPath, filename);

        string positionFile =
            System.IO.Path.Combine(positionPath, filename);

        using (StreamWriter sw = new StreamWriter(speedFile))
        using (StreamWriter sw2 = new StreamWriter(positionFile))
        {
            for (int i = 0; i < n; i++)
            {
                sw.WriteLine(
                    $"{speeds[i]}; " +
                    $"{accels[i]}; " +
                    $"{distances[i]}; " +
                    $"{ang_vel[i]}; " +
                    $"{ang_acc[i]}; " +
                    $"{kappas[i]}");

                Vector3 p = positions[i];
                Vector3 tangent = tans[i];

                sw2.WriteLine(
                    $"{p.x}; {p.y}; {p.z}; " +
                    $"{tangent.x}; {tangent.y}; {tangent.z}");
            }
        }

        Debug.Log(
            $"Successfully generated {n} samples. " +
            $"Final sampled distance={distances[n - 1]:F3} m");

        return true;
    }

    private static bool IsFinite(float x)
    {
        return !float.IsNaN(x) && !float.IsInfinity(x);
    }

    private static bool IsFinite(Vector3 v)
    {
        return IsFinite(v.x) &&
               IsFinite(v.y) &&
               IsFinite(v.z);
    }
    /*public bool generateFiles(string file, float totalTime)
    {
        string dir = System.IO.Path.GetDirectoryName(file);

        string speedPath = System.IO.Path.Combine(dir, "speeds");
        string positionPath = System.IO.Path.Combine(dir, "positions");
        string filename = System.IO.Path.GetFileName(file);

        if(!System.IO.Directory.Exists(speedPath)) 
        { 
            System.IO.Directory.CreateDirectory(speedPath);
        }

        if(!System.IO.Directory.Exists (positionPath))
        {
            System.IO.Directory.CreateDirectory(positionPath);
        }
        List<Bezier> beziers = new List<Bezier>();

        using (StreamReader streamReader = new StreamReader(file))
        {
            string line;
            while ((line = streamReader.ReadLine()) != null)
            {
                Bezier b = new Bezier(line);
                beziers.Add(b);
            }
        }

        List<SpeedSegment> segments = generateSpeedCurve(beziers);
        if (segments.Count == 0)
            return false;
        Debug.Log("Got segments");
        int segment_ix = 0;
        float distance = 0;
        float time_part = 0;
        List<float> distances = new List<float>() { 0};
        List<float> speeds = new List<float>() { 0 };
        List<float> accels = new List<float>() { 0};
        List<float> ang_vel = new List<float>() { 0};
        List<float> ang_acc = new List<float>() { 0};

        List<Vector3> positions  = new List<Vector3>() { beziers[0].start };
        List<Vector3> tans = new List<Vector3>() { beziers[0].first_derivative(0) };

        SpeedSegment segment = segments[segment_ix];
        List<float> kappas = new List<float>() { beziers[0].get_curvature(0) };
        Bezier bez = beziers[0];
        float bd = 0f;
        int n = Mathf.CeilToInt(totalTime * 10);
        Debug.Log($"Total steps: {n}");
        for (int t=0; t<n+1; t++)
        {
            float tf = ((float)t) / 10;
            time_part += 0.1f;
            float dist = 0f;
            bool partial = get_distance(segment, time_part - 0.1f, time_part, out dist);

            if (partial)
            {
                segment_ix += 1;
                if(time_part>0.0000001 && segment_ix<segments.Count)
                {
                    time_part -= segment.time;
                    segment = segments[Mathf.Min(segment_ix, segments.Count - 1)];

                    float new_dist = 0;
                    bool a = get_distance(segment, 0, time_part, out new_dist);
                    dist += new_dist;
                }

            }

            distance += dist;
            (bez, bd) = get_t_by_distance(beziers, distance);
            kappas.Add(bez.get_curvature(bd));
            if (kappas.Last() == float.NaN)
                Debug.LogError($"Kappa is nan: {bd}");
            speeds.Add(segment.calculate_speed(time_part));
            accels.Add(segment.get_acceleration());
            distances.Add(distance);
            


        }
        Debug.Log(kappas.Count);
        List<float> dKappaDt = new List<float>();
        float dt = 0.1f;
        for (int i = 0; i < n; i++)
        {
            ang_vel.Add(kappas[i] * speeds[i]);

            if (i == 0)
            {
                dKappaDt.Add((kappas[i + 1] - kappas[i]) / dt);
            }
            else if (i == n - 1)
            {
                dKappaDt.Add((kappas[i] - kappas[i - 1]) / dt);
            }
            else
            {
                dKappaDt.Add((kappas[i + 1] - kappas[i - 1]) / (2f * dt));
            }

            ang_acc.Add(dKappaDt[i] * speeds[i] + kappas[i] * accels[i]);

            (Vector3 pos, Vector3 tan) = get_position_by_distance(beziers, distances[i]);
            positions.Add(pos);
            tans.Add(tan);
        }


        using(StreamWriter sw = new StreamWriter(System.IO.Path.Combine(speedPath,filename)))
        {
            using (StreamWriter sw2 = new StreamWriter(System.IO.Path.Combine(positionPath, filename)))
            {
                for (int i = 0; i < n; i++)
                {
                    sw.WriteLine($"{speeds[i]}; {accels[i]}; {distances[i]}; {ang_vel[i]}; {ang_acc[i]}; {kappas[i]}");
                    Vector3 p = positions[i];
                    Vector3 t = tans[i];
                    sw2.WriteLine($"{p[0]}; {p[1]}; {p[2]}; {t[0]}; {t[1]}; {t[2]}");
                }
            }
        }

        return true;
        

    }*/

    public (Vector3,Vector3) get_position_by_distance(List<Bezier> beziers, float distance)
    {
        for(int i=0; i<beziers.Count;i++)
        {
            Bezier bez = beziers[i];
            if(distance<bez.length)
            {
                float t = bez.pointByDistance(distance);
                return (bez.getPoint(t),bez.first_derivative(t));

            }
            distance-=bez.length;

        }
        return (beziers.Last().end, beziers.Last().first_derivative(1));
    }

    public bool get_distance(SpeedSegment segment, float start, float end, out float distance) 
    {
        bool partial = false;
        if(end>=segment.time)
        {
            partial = true;
            end = segment.time;
        }
        distance = segment.calculate_distance(start, end);
        return partial;
    }

    

    public List<SpeedSegment> generateSpeedCurve(List<Bezier> beziers)
    {
        for(int tries = 0; tries < 100; tries++)
        {
            Debug.Log("Try number: " + tries);
            Debug.Log(total_length);
            var segments = prepare_segments();
            var accelerations = prepare_accelerations(segments.Count());
            List<SpeedSegment> profile = new List<SpeedSegment>();
            SpeedSegment last = new SpeedSegment(0, 0);
            float distance = 0f;

            bool fail = false;
            while(segments.Count > 0)
            {
                List<(float,SpeedSegment)> possible_segments = new List<(float,SpeedSegment)>();
                List<float> available_accelerations = accelerations.Distinct().ToList();

                foreach(SpeedSegment segment in segments) 
                { 
                    if(segment.s_speed == last.e_speed)
                    {
                        possible_segments.Add((0f, segment));
                    }
                    else
                    {
                        foreach(float acc in available_accelerations)
                        {
                            SpeedSegment[] testparts = generate_with_acceleration(last, segment, acc);
                            if (!check_segments(testparts, beziers, distance))
                                continue;
                            possible_segments.Add((acc, segment));
                        }

                    }
                }

                if (possible_segments.Count == 0)
                {
                    Debug.Log("No possible segments found");
                    fail = true;
                    break;
                }

                int rand = Random.Range(0, possible_segments.Count);

                float picked_acc = possible_segments[rand].Item1;
                SpeedSegment picked = possible_segments[rand].Item2;

                SpeedSegment[] parts = generate_with_acceleration(last,picked, picked_acc);
                
                segments.Remove(picked);
                accelerations.Remove(picked_acc);

                if (parts[0].time >0)
                    profile.Add(parts[0]);
                profile.Add(parts[1]);
                last = parts[1];

                distance += parts[0].length() + parts[1].length();

                //Debug.Log($"{distance}: {parts[1].e_speed} : {parts[1].time} : {parts[1].length()}");
            }
            if (fail)
                continue;

            profile.Last().time -= profile.Last().e_speed / default_acceleration;
            profile.Add(new SpeedSegment(profile.Last().e_speed, profile.Last().e_speed / default_acceleration, 0));

            List<SpeedSegment> final_profile = profile.Select(x => x.Clone()).ToList() ;
            Debug.Log(calculate_time(final_profile));
            float avg = calculate_average(final_profile);
            Debug.Log($"Current avg: {avg}  Target avg: {avg_speed}");
            /*int longest = 0;
            int shortest = 0;

            float long_length = 0f;
            float short_length = 99999f;

            float long_speed = d_speeds[0];
            float short_speed = d_speeds.Last();

            if(avg>avg_speed)
            {
                float temp = long_speed;
                long_speed = short_speed;
                short_speed = temp;
            }
            int j = 0;
           
            foreach(SpeedSegment s in final_profile)
            {
                if (close_enough(s.s_speed, long_speed) && close_enough(s.e_speed, s.s_speed))
                {
                    if (s.time > long_length)
                    {
                        long_length = s.time;
                        longest = j;
                    }
                }

                if (close_enough(s.s_speed, short_speed) && close_enough(s.e_speed, s.s_speed))
                {
                    if (s.time < short_length)
                    {
                        short_length = s.time;
                        shortest = j;
                    }
                }
                j++;
            }
            SpeedSegment lp = final_profile[longest];
            SpeedSegment sp = final_profile[shortest];

            profile.Remove(lp);
            profile.Remove(sp);

            float a0 = calculate_average(profile);
            float t0 = calculate_time(profile);

            float a1 = lp.s_speed;
            float t1 = lp.time;

            float a2 = sp.s_speed;
            float t2 = sp.time;

            float x = t1 + t2;
            float targetDistance = avg_speed * total_length;
            float t22 = (targetDistance - a1 * x - a0 * t0) / (a2 - a1);
            float t11 = x - t22;

            final_profile[longest].time = t11;
            final_profile[shortest].time = t22;
            if(t11<0 || t22<0)
            {
                fail = true;
                Debug.Log("Too short segments");
                Debug.Log($"t22: {t22}    x: {x}  t11: {t11}");

                continue;
            }*/


            AdjustAverageSpeed(final_profile, total_length, avg_speed);

            Debug.Log(calculate_average(final_profile));
            Debug.Log($"Time: {calculate_time(final_profile)}");
            Debug.Log($"Distance: {calculate_distance(final_profile)}");
            return final_profile;
        }

        return new List<SpeedSegment>();
    }


    public bool AdjustAverageSpeed(
    List<SpeedSegment> profile,
    float targetTime,
    float targetAverage)
    {
        float currentTime = profile.Sum(s => s.time);
        float currentDistance = profile.Sum(s => s.length());

        // This method only redistributes existing time.
        if (Mathf.Abs(currentTime - targetTime) > 0.001f)
        {
            Debug.LogError(
                $"Profile does not have target time! " +
                $"Current={currentTime}, target={targetTime}");
            return false;
        }

        float targetDistance = targetAverage * targetTime;
        float requiredDistanceChange =
            targetDistance - currentDistance;

        var adjustable = profile
            .Where(s => close_enough(s.s_speed, s.e_speed))
            .ToList();

        if (adjustable.Count < 2)
            return false;

        // Arithmetic mean of speeds
        float meanSpeed =
            adjustable.Average(s => s.s_speed);

        /*
         * Choose:
         *
         * delta_t[i] = lambda * (v[i] - meanSpeed)
         *
         * This guarantees:
         *
         * sum(delta_t) = 0
         *
         * and therefore total time cannot change.
         */

        float denominator = 0f;

        foreach (SpeedSegment s in adjustable)
        {
            float dv = s.s_speed - meanSpeed;
            denominator += s.s_speed * dv;
        }

        if (Mathf.Abs(denominator) < 1e-8f)
            return false;

        float lambda =
            requiredDistanceChange / denominator;

        // Calculate first without changing anything
        float[] newTimes = new float[adjustable.Count];

        for (int i = 0; i < adjustable.Count; i++)
        {
            float dv =
                adjustable[i].s_speed - meanSpeed;

            newTimes[i] =
                adjustable[i].time + lambda * dv;

            if (newTimes[i] < 0f)
            {
                Debug.LogError(
                    $"Impossible adjustment. Segment {i} " +
                    $"would have time {newTimes[i]}");

                return false;
            }
        }

        // Apply
        for (int i = 0; i < adjustable.Count; i++)
            adjustable[i].time = newTimes[i];

        // Verify
        float finalTime =
            profile.Sum(s => s.time);

        float finalDistance =
            profile.Sum(s => s.length());

        float finalAverage =
            finalDistance / finalTime;

        Debug.Log(
            $"Before: T={currentTime}, D={currentDistance}, " +
            $"avg={currentDistance / currentTime}\n" +
            $"After:  T={finalTime}, D={finalDistance}, " +
            $"avg={finalAverage}\n" +
            $"Targets: T={targetTime}, D={targetDistance}, " +
            $"avg={targetAverage}");

        return true;
    }

    public bool close_enough(float a, float b)
    {
        return (Mathf.Abs(a - b) < 0.1f);
    }

    /*
    public List<SpeedSegment> prepare_segments()
    {
        List<SpeedSegment> toReturn = new List<SpeedSegment>();
        float single_length = total_length / (d_speeds.Length * segment_count)/2;
        float total = 0;
        foreach(float s in d_speeds)
        {
            for(int i=0; i<=(int)Mathf.Ceil(segment_count/2); i++)
            {
                float rl = Random.Range(0f, single_length*0.5f);
                toReturn.Add(new SpeedSegment(s, (single_length + rl)/s));
                toReturn.Add(new SpeedSegment(s, (single_length - rl)/s));
                total += single_length;

            }
        }

        Debug.Log(total);
        return toReturn;
    }*/

    public List<SpeedSegment> prepare_segments()
    {
        List<SpeedSegment> result = new List<SpeedSegment>();

        float timePerSpeed =
            total_length / d_speeds.Length;

        float segmentTime =
            timePerSpeed / segment_count;

        foreach (float speed in d_speeds)
        {
            int pairs = segment_count / 2;

            for (int i = 0; i < pairs; i++)
            {
                float variation =
                    Random.Range(0f, segmentTime * 0.5f);

                result.Add(
                    new SpeedSegment(
                        speed,
                        segmentTime + variation));

                result.Add(
                    new SpeedSegment(
                        speed,
                        segmentTime - variation));
            }

            // Odd number of segments
            if (segment_count % 2 != 0)
            {
                result.Add(
                    new SpeedSegment(speed, segmentTime));
            }
        }

        return result;
    }

    public List<float> prepare_accelerations(int count)
    {
        List<float> toReturn = new List<float>();
        

        int upper = (int)(Mathf.Ceil(count / d_accelerations.Length));
        for(int i=0; i<upper; i++)
        {
            toReturn.AddRange(d_accelerations);
        }
        return toReturn;
    }

    public (Bezier,float) get_t_by_distance(List<Bezier> beziers, float distance)
    {
        foreach(Bezier b in beziers)
        {
            if (b.length > distance)
            {
                float t = b.t_by_distance(distance);
                return (b, t);
            }
        }
        return (beziers.Last() as Bezier, 1.0f);
    }


    public SpeedSegment[] generate_with_acceleration(SpeedSegment prev, SpeedSegment next, float acc)
    {
        float acc_t = Mathf.Abs(next.s_speed - prev.e_speed) / acc;
        if(acc_t == 0f || acc == 0f)
        {
            return new SpeedSegment[] { new SpeedSegment(prev.s_speed,0), next };
        }

        return new SpeedSegment[] { new SpeedSegment(prev.e_speed, acc_t, next.s_speed), new SpeedSegment(next.s_speed, next.time-acc_t)};
    }

    public bool check_segments(SpeedSegment[] segments, List<Bezier> beziers, float start_distance)
    {
        foreach(SpeedSegment segment in segments)
        {
            for(int i=1; i<41; i++)
            {
                float last = (((float)i - 1.0f) / 40f) * segment.time;
                float next = ((float)i) / 40f * segment.time;

                start_distance += segment.calculate_distance(last, next);

                float speed = segment.calculate_speed(next);

                (Bezier b, float t) = get_t_by_distance(beziers, start_distance);

                (Bezier pc, float pt) = get_t_by_distance(beziers, start_distance - speed * 0.05f);
                (Bezier nc, float nt) = get_t_by_distance(beziers, start_distance + speed * 0.05f);
                float kk = (nc.get_curvature(nt) - pc.get_curvature(pt)) / 0.1f;

                float acc = kk * speed + b.get_curvature(t) * segment.get_acceleration();

                float radius = b.get_curvature_radius(t);

                float ang_vel = speed / radius;

                if (Mathf.Abs(ang_vel) > max_radians_per_second || Mathf.Abs(acc) > max_ang_acc)
                    return false;
            }
        }
        return true;
    }

    public float calculate_distance(List<SpeedSegment> segments)
    {
        float d = 0;
        foreach(SpeedSegment segment in segments)
        {
            d += segment.length();
        }
        return d;
    }

    public float calculate_time(List<SpeedSegment> profile)
    {
        float l = 0;
        foreach(SpeedSegment segment in profile)
        {
            l += segment.time;
        }
        return l;
    }

    public float calculate_average(List<SpeedSegment> profile)
    {
        float time = 0;
        float dist = 0;
        foreach(SpeedSegment segment in profile)
        {
            time += segment.time;
            dist += segment.length();
        }
        return dist / time;
    }

    public List<(float,float)> speed_profile_to_distance_curve(List<SpeedSegment> profile)
    {
        float dist = 0;
        float time = 0;

        List<(float,float)> speeds = new List<(float,float)>();
        speeds.Add((0,0));

        foreach(SpeedSegment segment in profile)
        {
            time = segment.time;
            speeds.Add((time, segment.e_speed));
        }
        return speeds;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [EditorCools.Button]
    public void tester()
    {

    }

    public class SpeedSegment
    {
        public float s_speed;
        public float e_speed;
        public float time;

        public SpeedSegment(float s, float t, float es = -1)
        {
            if (es < 0)
                es = s;
            s_speed = s;
            e_speed = es;
            time = t;
        }

        public float calculate_distance(float start, float end)
        {
            if (time == 0)
                return 0;

            float sd = start / time;
            float ed = end / time;

            float sp = s_speed + sd * (e_speed - s_speed);
            float ep = s_speed + ed * (e_speed - s_speed);
            return ((sp + ep) / 2) * (end - start);
        }

        public float calculate_speed(float t)
        {
            if (time == 0) return s_speed;
            return s_speed + (e_speed - s_speed) * (t / time);
        }

        public float get_acceleration()
        {
            return (e_speed - s_speed) / time;
        }

        public float length()
        {
            if (time==0) return 0;
            return calculate_distance(0, time);
        }

        public SpeedSegment Clone()
        {
            return new SpeedSegment(s_speed, time, e_speed);
        }
    }
}
