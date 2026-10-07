using Accord.Math;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

public class GenerateSpeedProfile : MonoBehaviour
{
    public float maxDegreesPerSecond = 90f;
    public float maxSpeed = 1f;
    float[] MaxSpeeds;
    public AnimationCurve MaxSpeedCurve;
    public float[] possibleSpeeds = new float[] { 0.25f, 0.5f, 0.75f, 1.0f };
    public float[] possibleAccelerations = new float[] { 0.25f, 0.5f, 0.75f, 1.0f };
    public int segmentCount = 8;
    public Vector2 lengthLimits = new Vector2(0, 5);
    public float averageSpeed = 0.5f;
    public float acceleration = 0.5f;
    public float totalTime = 300;

    public List<SpeedSegment> finalProfile = new List<SpeedSegment>();
    
    

    public void generateRamps()
    {
        List<SpeedSegment> newList = new List<SpeedSegment>();
        
        SpeedSegment start = new SpeedSegment(0, finalProfile[0].startSpeed, finalProfile[0].startSpeed / acceleration);
        newList.Add(start);
        newList.Add(new SpeedSegment(finalProfile[0].startSpeed, finalProfile[0].time-start.time));
        for (int i = 1; i < finalProfile.Count; i++)
        {
            SpeedSegment s = finalProfile[i];
            SpeedSegment p = finalProfile[i - 1];

            if (s.startSpeed != p.startSpeed)
            {
                SpeedSegment change = new SpeedSegment(p.endSpeed, s.startSpeed, Mathf.Abs(s.startSpeed - p.endSpeed) / acceleration);
                newList.Add(change);
                newList.Add(new SpeedSegment(s.startSpeed, s.time - change.time));
            }
            else
            {
                newList.Add(s);
            }
        }
        SpeedSegment last = newList[newList.Count - 1];
        newList[newList.Count - 1].time -= last.endSpeed/acceleration;
        newList.Add(new SpeedSegment(newList[newList.Count - 1].endSpeed, 0, newList[newList.Count - 1].endSpeed / acceleration));
        finalProfile = newList; 
    }

    public float calculateAverage(List<SpeedSegment> pf)
    {
        float d = 0;
        float avg = 0;
        foreach (SpeedSegment s in pf)
        {
            d += s.time;
            avg += s.calculateDistance(0,s.time);
        }
        return avg / d;
    }

    struct SegmentWithAcceleration
    {
        public SpeedSegment s;
        public float acc;

        public SegmentWithAcceleration(SpeedSegment s, float acc)
        {
            this.s = s;
            this.acc = acc;
        }
    }

    public struct speedCurveResponse
    {
        public AnimationCurve distanceCurve, speedCurve;
        public bool ready;
        public bool success;
    }

    public speedCurveResponse speedResonse = new speedCurveResponse();

    public IEnumerator generateSpeedCurveAsync(rMap map)
    {
        speedResonse.ready = false;
        speedResonse.success = false;
        yield return null;
        for (int tries = 0; tries < 100; tries++)
        {
            List<SpeedSegment> segments = prepareSegments(totalTime);
            List<float> accelerations = prepareAccelerations(segments.Count);
            List<SpeedSegment> profile = new List<SpeedSegment>();
            SpeedSegment last = new SpeedSegment(0, 0);
            float distanceTravelled = 0;
            Debug.Log("Here");
            while (segments.Count > 0)
            {
                List<SegmentWithAcceleration> possibleSegments = new List<SegmentWithAcceleration>();
                List<float> availableAccelerations = accelerations.Distinct().ToList();
                foreach (SpeedSegment segment in segments)
                {

                    if (segment.startSpeed == last.endSpeed && segments.Count != 1)
                        continue;
                    foreach (float acceleration in availableAccelerations)
                    {
                        var (ramp, next) = generateWithAcceleration(last, segment, acceleration);
                        List<SpeedSegment> sss = new List<SpeedSegment>();
                        if (ramp.time > 0) sss.Add(ramp);
                        sss.Add(next);
                        if (!checkSegments(sss.ToArray(), map, distanceTravelled))
                            continue;
                        possibleSegments.Add(new SegmentWithAcceleration(segment, acceleration));

                    }
                }

                if (possibleSegments.Count == 0)
                {
                    Debug.Log("No possible segments found at: " + distanceTravelled + ", " + profile.Count);
                    foreach (SpeedSegment s in segments)
                        Debug.Log($"{s.startSpeed} - {s.time}");
                    break;
                }

                SegmentWithAcceleration picked = possibleSegments[Random.Range(0, possibleSegments.Count)];
                var (newramp, newnext) = generateWithAcceleration(last, picked.s, picked.acc);
                segments.Remove(picked.s);
                accelerations.Remove(picked.acc);
                if (newramp.time > 0)
                    profile.Add(newramp);
                profile.Add(newnext);
                last = newnext;
                distanceTravelled += newramp.calculateDistance() + newnext.calculateDistance();
                yield return null;
            }
            if (segments.Count > 0)
                continue;
            profile[profile.Count - 1].time -= last.endSpeed / acceleration;
            profile.Add(new SpeedSegment(last.endSpeed, 0, last.endSpeed / acceleration));

            finalProfile = profile;
            float avg = calculateAverage(finalProfile);
            int longest = 0;
            int shortest = 0;
            float longLength = 0;
            float shortLength = 9999;

            float longSpeed = 0.25f;
            float shortSpeed = 1.50f;
            if (avg > averageSpeed)
            {
                longSpeed = 1.5f;
                shortSpeed = 0.25f;
            }



            int j = 0;
            foreach (SpeedSegment segment in finalProfile)
            {
                if (Mathf.Abs(segment.startSpeed - longSpeed) < 0.1f && segment.endSpeed == segment.startSpeed)
                {
                    if (segment.time > longLength)
                    {
                        longLength = segment.time;
                        longest = j;
                        //Debug.Log("Longest at " + j);
                    }

                }
                if (Mathf.Abs(segment.startSpeed - shortSpeed) < 0.1f && segment.endSpeed == segment.startSpeed)
                {
                    if (segment.time < shortLength)
                    {
                        shortLength = segment.time;
                        shortest = j;
                        //Debug.Log("Shortest at " + j);
                    }
                }
                yield return null;

                j++;
            }

            List<SpeedSegment> withoutShortLong = new List<SpeedSegment>(finalProfile);
            withoutShortLong.Remove(finalProfile[longest]);
            withoutShortLong.Remove(finalProfile[shortest]);

            float a0 = calculateAverage(withoutShortLong);
            float t0 = calculateLength(withoutShortLong);

            float a1 = finalProfile[longest].startSpeed;
            float t1 = finalProfile[longest].time;

            float a2 = finalProfile[shortest].startSpeed;
            float t2 = finalProfile[shortest].time;
            float x = t1 + t2;

            float t22 = (averageSpeed * totalTime - a1 * x - a0 * t0) / (a2 - a1);

            float t11 = x - t22;

            finalProfile[longest].time = t11;
            finalProfile[shortest].time = t22;
            Debug.Log($"{avg}");
            if (t11 < 0 || t22 < 0)
            {
                Debug.LogWarning($"t11: {t11} t22: {t22}");
                continue;
            }
            (AnimationCurve distance, AnimationCurve speed, bool success) = speedProfileToDistanceCurve();
            speedResonse.distanceCurve = distance;
            speedResonse.speedCurve = speed;
            speedResonse.success = success;
            speedResonse.ready = true;
            //return speedProfileToDistanceCurve();

        }
        
        if(!speedResonse.success)
        {
            speedResonse.distanceCurve = new AnimationCurve();
            speedResonse.speedCurve = new AnimationCurve();
            speedResonse.success = false;
            speedResonse.ready = true;
            //return (new AnimationCurve(), new AnimationCurve(), false);

        }
    }


    public (AnimationCurve, AnimationCurve, bool) generateSpeedCurveV2(rMap map)
    {
        for(int tries = 0; tries<100; tries++)
        {
            List<SpeedSegment> segments = prepareSegments(totalTime);
            List<float> accelerations = prepareAccelerations(segments.Count);
            List<SpeedSegment> profile = new List<SpeedSegment>();
            SpeedSegment last = new SpeedSegment(0, 0);
            float distanceTravelled = 0;
            Debug.Log("Here");
            while (segments.Count > 0)
            {
                List<SegmentWithAcceleration> possibleSegments = new List<SegmentWithAcceleration>();
                List<float> availableAccelerations = accelerations.Distinct().ToList();
                foreach (SpeedSegment segment in segments)
                {

                    if (segment.startSpeed == last.endSpeed && segments.Count!=1)
                        continue;
                    foreach (float acceleration in availableAccelerations)
                    {
                        var (ramp, next) = generateWithAcceleration(last, segment,acceleration);
                        List<SpeedSegment> sss = new List<SpeedSegment>();
                        if(ramp.time>0) sss.Add(ramp);
                        sss.Add(next);
                        if (!checkSegments(sss.ToArray(), map, distanceTravelled))
                            continue;
                        possibleSegments.Add(new SegmentWithAcceleration(segment,acceleration));

                    }
                }

                if (possibleSegments.Count == 0)
                {
                    Debug.Log("No possible segments found at: " + distanceTravelled + ", " + profile.Count);
                    foreach(SpeedSegment s in segments)
                        Debug.Log($"{s.startSpeed} - {s.time}");
                    break;
                }

                SegmentWithAcceleration picked = possibleSegments[Random.Range(0, possibleSegments.Count)];
                var (newramp, newnext) = generateWithAcceleration(last, picked.s, picked.acc);
                segments.Remove(picked.s);
                accelerations.Remove(picked.acc);
                if(newramp.time>0)
                    profile.Add(newramp);
                profile.Add(newnext);
                last = newnext;
                distanceTravelled += newramp.calculateDistance() + newnext.calculateDistance();
            }
            if (segments.Count > 0)
                continue;
            profile[profile.Count - 1].time -= last.endSpeed / acceleration;
            profile.Add(new SpeedSegment(last.endSpeed, 0, last.endSpeed / acceleration));

            finalProfile = profile;
            float avg = calculateAverage(finalProfile);
            int longest = 0;
            int shortest = 0;
            float longLength = 0;
            float shortLength = 9999;

            float longSpeed = 0.25f;
            float shortSpeed = 1.50f;
            if (avg > averageSpeed)
            {
                longSpeed = 1.5f;
                shortSpeed = 0.25f;
            }



            int j = 0;
            foreach (SpeedSegment segment in finalProfile)
            {
                if (Mathf.Abs(segment.startSpeed - longSpeed) < 0.1f && segment.endSpeed == segment.startSpeed)
                {
                    if (segment.time > longLength)
                    {
                        longLength = segment.time;
                        longest = j;
                        //Debug.Log("Longest at " + j);
                    }

                }
                if (Mathf.Abs(segment.startSpeed - shortSpeed) < 0.1f && segment.endSpeed == segment.startSpeed)
                {
                    if (segment.time < shortLength)
                    {
                        shortLength = segment.time;
                        shortest = j;
                        //Debug.Log("Shortest at " + j);
                    }
                }

                j++;
            }

            List<SpeedSegment> withoutShortLong = new List<SpeedSegment>(finalProfile);
            withoutShortLong.Remove(finalProfile[longest]);
            withoutShortLong.Remove(finalProfile[shortest]);

            float a0 = calculateAverage(withoutShortLong);
            float t0 = calculateLength(withoutShortLong);

            float a1 = finalProfile[longest].startSpeed;
            float t1 = finalProfile[longest].time;

            float a2 = finalProfile[shortest].startSpeed;
            float t2 = finalProfile[shortest].time;
            float x = t1 + t2;

            float t22 = (averageSpeed * totalTime - a1 * x - a0 * t0) / (a2 - a1);

            float t11 = x - t22;

            finalProfile[longest].time = t11;
            finalProfile[shortest].time = t22;
            Debug.Log($"{avg}");
            if (t11 < 0 || t22 < 0)
            {
                Debug.LogWarning($"t11: {t11} t22: {t22}");
                continue;
            }

            return speedProfileToDistanceCurve();

        }
        return (new AnimationCurve(), new AnimationCurve(), false);
    }

    public (SpeedSegment, SpeedSegment) generateWithAcceleration(SpeedSegment last, SpeedSegment next, float acceleration)
    {
        float accelerationTime = Mathf.Abs(next.startSpeed - last.endSpeed) / acceleration;
        return (new SpeedSegment(last.endSpeed, next.startSpeed, accelerationTime), new SpeedSegment(next.startSpeed, next.time - accelerationTime));
    }

    public bool checkSegments(SpeedSegment[] segments, rMap map, float startDistance)
    {
        //Debug.Log($"Start distance{startDistance}");
        
        foreach (SpeedSegment segment in segments)
        {
            for(int i =1; i<=40; i++)
            {
                float last = ((float)i - 1.0f) / 40 * segment.time;
                float next = ((float)i)/40*segment.time;
                startDistance += segment.calculateDistance(last, next);
                //Debug.Log($"{last}, {next}, {segment.calculateDistance(last, next)}, {segment.time}");
                float speed = segment.calculateSpeed(next);
                var point = map.getPointByDistance(startDistance);
                
                float realRadius = rHelpers.GetCurvatureRadius(point.b.start, point.b.C1, point.b.C2, point.b.end, point.t);
                

                float angularVelocity = speed / realRadius;


                if (Mathf.Abs(angularVelocity) > maxDegreesPerSecond)
                {
                    Debug.Log($"Cannot do it!! {speed}, {realRadius}, {angularVelocity}");
                    return false;
                }
            }
        }
        return true;
    }

    [EditorCools.Button]
    public void checkBezier()
    {
        UnityEngine.Vector3 P0 = UnityEngine.Vector3.zero;
        UnityEngine.Vector3 P1 = new UnityEngine.Vector3(1, 0, 2);
        UnityEngine.Vector3 P2 = new UnityEngine.Vector3(2, 0, 2);
        UnityEngine.Vector3 P3 = new UnityEngine.Vector3(3, 0, 0);

    }
    public AnimationCurve generateSpeedCurve()
    {

        for (int tries = 0; tries < 200; tries++)
        {
            List<SpeedSegment> segments = prepareSegments(totalTime);
            List<SpeedSegment> profile = new List<SpeedSegment>();
            SpeedSegment last = new SpeedSegment(0, 0);
            int index = 0;
            while (segments.Count > 0)
            {
                List<SpeedSegment> possibleSegments = new List<SpeedSegment>();
                foreach (SpeedSegment segment in segments)
                {
                    float segmentDist = segment.time * segment.startSpeed;
                    int segmentLen = Mathf.FloorToInt(segmentDist / 0.1f);
                    bool possible = true;
                    if(segment.startSpeed == last.startSpeed)
                        possible = false;
                    else
                    {
                        if(index+segmentLen>=MaxSpeeds.Length)
                        {
                            segmentLen = MaxSpeeds.Length - index;
                        }
                        for (int i = index; i < index + segmentLen; i++)
                        {
                            
                            if (MaxSpeeds[i] < segment.startSpeed)
                            {
                                possible = false;
                                Debug.Log($"Failed at {i}  {MaxSpeeds[i]} < {segment.startSpeed}");
                                break;
                            }
                        }

                    }

                    if (possible)
                    {
                        possibleSegments.Add(segment);
                    }
                }

                if(possibleSegments.Count == 0)
                {
                    break;
                }

                SpeedSegment next = possibleSegments[Random.Range(0,possibleSegments.Count)];
                segments.Remove(next);
                profile.Add(next);
                last = next;
                float sd = next.time * next.startSpeed;
                int sl = Mathf.FloorToInt(sd / 0.1f);
                index += sl;

            }
            if (segments.Count > 0)
            {
                Debug.Log($"Fail: {segments.Count}");
            }
            else
            {
                finalProfile = profile;
                generateRamps();
                float avg = calculateAverage(finalProfile);
                int longest = 0;
                int shortest = 0;
                float longLength = 0;
                float shortLength = 9999;

                float longSpeed = 0.25f;
                float shortSpeed = 1.50f;
                if(avg>averageSpeed)
                {
                    longSpeed = 1.5f;
                    shortSpeed = 0.25f;
                }


                
                int j = 0;
                foreach (SpeedSegment segment in finalProfile)
                {
                    if(Mathf.Abs(segment.startSpeed - longSpeed)<0.1f && segment.endSpeed == segment.startSpeed)
                    {
                        if (segment.time > longLength)
                        {
                            longLength = segment.time;
                            longest = j;
                            //Debug.Log("Longest at " + j);
                        }

                    }
                    if(Mathf.Abs(segment.startSpeed - shortSpeed) < 0.1f && segment.endSpeed ==segment.startSpeed)
                    {
                        if(segment.time < shortLength)
                        {
                            shortLength = segment.time;
                            shortest = j;
                            //Debug.Log("Shortest at " + j);
                        }
                    }

                    j++;
                }

                List<SpeedSegment> withoutShortLong = new List<SpeedSegment>(finalProfile);
                withoutShortLong.Remove(finalProfile[longest]);
                withoutShortLong.Remove(finalProfile[shortest]);

                float a0 = calculateAverage(withoutShortLong);
                float t0 = calculateLength(withoutShortLong);

                float a1 = finalProfile[longest].startSpeed;
                float t1 = finalProfile[longest].time;

                float a2 = finalProfile[shortest].startSpeed;
                float t2=finalProfile[shortest].time;
                float x = t1 + t2;

                float t22 = (averageSpeed * totalTime - a1 * x - a0 * t0) / (a2 - a1);

                float t11 = x - t22;

                finalProfile[longest].time = t11;
                finalProfile[shortest].time = t22;

                if(t11<0 || t22<0)
                {
                    Debug.LogWarning($"t11: {t11} t22: {t22}");
                    continue;
                }

                //return speedProfileToDistanceCurve();
            }
        }
        return new AnimationCurve();
    }

    public (AnimationCurve, AnimationCurve, bool) speedProfileToDistanceCurve(float resolution = 0.05f)
    {
        float distanceTravelled = 0;
        float timeTravelled = 0;
        AnimationCurve curve = new AnimationCurve();
        Keyframe key = new Keyframe(0, 0);
        curve.AddKey(key);
        AnimationCurve speedCurve = new AnimationCurve();
        speedCurve.AddKey(new Keyframe(0, 0));
        foreach(SpeedSegment segment in finalProfile)
        {
            distanceTravelled += segment.calculateDistance();
            timeTravelled += segment.time;
            curve.AddKey(new Keyframe(timeTravelled, distanceTravelled));
            speedCurve.AddKey(new Keyframe(timeTravelled, segment.endSpeed));
        }

        for (int i = 1; i < curve.keys.Length-1; i++)
        {
            Keyframe k = curve.keys[i];
            if(i%2==1)
            {
                Keyframe next = curve.keys[i + 1];
                Keyframe current = curve.keys[i];
                Vector2 tangent = new Vector2(next.time - current.time, next.value - current.value);
                k.outTangent = tangent.y / tangent.x;
                k.inTangent = tangent.y / tangent.x;
            }
            else
            {
                Keyframe next = curve.keys[i];
                Keyframe current = curve.keys[i-1];
                Vector2 tangent = new Vector2(next.time - current.time, next.value - current.value);
                k.outTangent = tangent.y / tangent.x;
                k.inTangent = tangent.y / tangent.x;
            }
            curve.MoveKey(i, k);

        }
#if UNITY_EDITOR
        for(int i=0; i<speedCurve.keys.Length; i++)
        {
            Keyframe k = speedCurve.keys[i];
            AnimationUtility.SetKeyLeftTangentMode(speedCurve, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(speedCurve, i, AnimationUtility.TangentMode.Linear);
        }
#endif
        return (curve, speedCurve, true);
        /*
        float[] distance = profileToDistanceTravelled(resolution);
        AnimationCurve toReturn = new AnimationCurve();
        float t = 0;
        for(int i=0; i< distance.Length;i++)
        {
            toReturn.AddKey(t, distance[i]);
            t += resolution;
        }
        return toReturn;*/
    }

    public List<float> trajectoryFromSpeedProfiles(List<UnityEngine.Vector3> trajectory, float resolution =0.05f)
    {
        float distanceTravelled = 0;
        float[] distance = profileToDistanceTravelled(resolution);
        List<float> positions = new List<float>();
        positions.Add(0);
        int lastIndex = 0;
        UnityEngine.Vector3 lastPosition = trajectory[0];
        float lastD = positions[0];
        for (int d=1; d<distance.Length; d++)
        {
            float targetDistance = distance[d] - distance[d-1];
            bool found = false;
            for(int i = lastIndex; i<trajectory.Count; i++)
            {
                float dist = (trajectory[i] - lastPosition).magnitude;
                if(dist>targetDistance)
                {
                    UnityEngine.Vector3 newPoint = rHelpers.pointOnLine(lastPosition, trajectory[i - 1], trajectory[i], targetDistance);
                    float f = (newPoint - trajectory[i - 1]).magnitude / (trajectory[i]-trajectory[i-1]).magnitude;
                    positions.Add((float)i + f);
                    lastPosition = newPoint;
                    lastIndex = i;
                    found = true;
                    break;
                }
            }
            if(!found)
            {
                Debug.Log("Reached the end");
                break;
            }
            
        }
        return positions;

    }

    
    public float[] profileToDistanceTravelled(float resolution = 0.01f)
    {
        
        float[] toReturn = new float[Mathf.FloorToInt(totalTime / resolution)+1];
        float d = 0;
        int index = 0;
        float totalDistance = 0;
        int i = 0;
        for (i=0; i< Mathf.FloorToInt(totalTime / resolution); i++)
        {
            float time = i * resolution;
            toReturn[i] = totalDistance;
            float timeOnProfile = time - d;
            //Debug.Log($"Time: {time} d:{d} timeOnProfile:{timeOnProfile}");
            if (timeOnProfile + resolution > finalProfile[index].time)
            {
                //Changing profiles midway
                float sd = finalProfile[index].calculateDistance(timeOnProfile, finalProfile[index].time);
                if(sd<0)
                    Debug.LogWarning("sd: "+sd+", " + timeOnProfile + "," + finalProfile[index].time);
                d += finalProfile[index].time;
                timeOnProfile = time - d;
                index++;
                if(index==finalProfile.Count)
                {
                    totalDistance += sd;
                    break;
                }
                float ed = finalProfile[index].calculateDistance(timeOnProfile, timeOnProfile + resolution);
                if(ed<0)
                    Debug.LogWarning("ed: " + ed + ", " + timeOnProfile);
                totalDistance += sd + ed;
            }
            else
            {
                //Same profile
                float dd = finalProfile[index].calculateDistance(timeOnProfile, timeOnProfile + resolution);
                totalDistance += dd;
                if(dd<0)
                    Debug.LogWarning("d: "+ dd + ", " + timeOnProfile);
                
                
            }
        }
        toReturn[toReturn.Length - 1] = totalDistance;
        return toReturn;
    }
    public class SpeedTrajectory
    {
        public float[] speeds;
        public float[] positions;

        public SpeedTrajectory(float[] speeds, float[] positions)
        {
            this.speeds = speeds;
            this.positions = positions;
        }
    }
    public float calculateLength(List<SpeedSegment> pf)
    {
        float length = 0;
        foreach (SpeedSegment segment in pf)
            length += segment.time;
        return length;
    }
    
    public void CalculateMaxSpeeds(float[] yaws, float resolution = 0.1f)
    {
        List<float> maxes = new List<float>();

        foreach(float curvature in yaws)
        {
            float velocity = (curvature != 0) ? maxDegreesPerSecond / Mathf.Abs(curvature) : maxSpeed;
            maxes.Add(Mathf.Clamp(velocity,0,maxSpeed));
        }

    }

    public List<SpeedSegment> prepareSegments(float totalLength)
    {
        List<SpeedSegment> toReturn = new List<SpeedSegment>();

        float singleLength = totalLength / (possibleSpeeds.Length*segmentCount);
        Debug.Log("Length without random: "+singleLength);
        foreach(float speed in possibleSpeeds)
        {
            for(int i=0; i<segmentCount/2; i++)
            {
                float randomLength = Random.Range(lengthLimits.x, lengthLimits.y);
                toReturn.Add(new SpeedSegment(speed, singleLength + randomLength));
                toReturn.Add(new SpeedSegment(speed, singleLength - randomLength));
                
            }
        }

        return toReturn;
    }

    public List<float> prepareAccelerations(int count)
    {
        List<float> toReturn = new List<float>();

        int upperCount = Mathf.CeilToInt((float)count / possibleAccelerations.Length);
        for (int i=0; i<upperCount;i++)
        {
            toReturn.AddRange(possibleAccelerations);
        }
        return toReturn;


    }

    [System.Serializable]
    public class SpeedSegment
    {
        public float startSpeed;
        public float endSpeed;
        public float time;

        public SpeedSegment(float speed, float time)
        {
            this.startSpeed = speed;
            this.endSpeed = speed;
            this.time = time;
        }

        public SpeedSegment(float sSpeed, float eSpeed, float d)
        {
            startSpeed = sSpeed;
            endSpeed = eSpeed;
            time = d;
        }

        public float calculateDistance(float start, float end)
        {
            float sd = start / time;
            float ed = end / time;

            float sp = startSpeed + sd * (endSpeed - startSpeed);
            float ep = startSpeed + ed * (endSpeed - startSpeed);
            return ((sp + ep) / 2) * (end - start);
        }

        public float calculateSpeed(float t)
        {
            return startSpeed + (endSpeed - startSpeed)*t/time;
        }

        public float calculateDistance()
        {
            return calculateDistance(0, time);
        }
    }
}
