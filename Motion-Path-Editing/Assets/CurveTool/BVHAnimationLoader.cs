using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using burningmime.curves;
public class BVHAnimationLoader : MonoBehaviour {
    [Header("Loader settings")]
    [Tooltip("This is the target avatar for which the animation should be loaded. Bone names should be identical to those in the BVH file and unique. All bones should be initialized with zero rotations. This is usually the case for VRM avatars.")]
    public Animator targetAvatar;
    [Tooltip("This is the path to the BVH file that should be loaded. Bone offsets are currently being ignored by this loader.")]
    public string filename;
    [Tooltip("When this option is set, the BVH file will be assumed to have the Z axis as up and the Y axis as forward instead of the normal BVH conventions.")]
    public bool blender = true;
    [Tooltip("When this flag is set, the frame time in the BVH time will be used to determine the frame rate instead of using the one given below.")]
    public bool respectBVHTime = true;
    [Tooltip("If the flag above is disabled, the frame rate given in the BVH file will be overridden by this value.")]
    public float frameRate = 60.0f;
    [Tooltip("This is the name that will be set on the animation clip. Leaving this empty is also okay.")]
    public string clipName;
    [Header("Advanced settings")]
    [Tooltip("When this option is enabled, standard Unity humanoid bone names will be mapped to the corresponding bones of the skeleton.")]
    public bool standardBoneNames = true;
    [Tooltip("When this option is disabled, bone names have to match exactly.")]
    public bool flexibleBoneNames = true;
    [Tooltip("This allows you to give a mapping from names in the BVH file to actual bone names. If standard bone names are enabled, the target names may also be Unity humanoid bone names. Entries with empty BVH names will be ignored.")]
    public FakeDictionary[] boneRenamingMap = null;
    [Header("Animation settings")]
    [Tooltip("When this option is set, the animation start playing automatically after being loaded.")]
    public bool autoPlay = false;
    [Tooltip("When this option is set, the animation will be loaded and start playing as soon as the script starts running. This also implies the option above being enabled.")]
    public bool autoStart = false;
    [Header("Animation")]
    [Tooltip("This is the Animation component to which the clip will be added. If left empty, a new Animation component will be added to the target avatar.")]
    public Animation anim;
    [Tooltip("This field can be used to read out the the animation clip after being loaded. A new clip will always be created when loading.")]
    public AnimationClip clip;

    static private int clipCount = 0;
    public BVHParser bp = null;
    private Transform rootBone;
    private string prefix;
    private int frames;
    private Dictionary<string, string> pathToBone;
    private Dictionary<string, string[]> boneToMuscles;
    private Dictionary<string, Transform> nameMap;
    private Dictionary<string, string> renamingMap;
    List<GameObject> line = new List<GameObject>();
    public float error = 0.03f;
    public float error2 = 3;
    List<Vector3> pos = new List<Vector3>();
    List<Vector3> rot = new List<Vector3>();
    List<float> arcLen = new List<float>();
    List<Vector3> pts = new List<Vector3>();
    List<Vector3> dpts = new List<Vector3>();
    List<GameObject> cpts = new List<GameObject>();
    List<burningmime.curves.CubicBezier> curves = new List<burningmime.curves.CubicBezier>();
    [Range(0, 359)]
    public int frameIndex = 0;
    public GameObject front;
    public GameObject cptPrefab;
    [Serializable]
    public struct FakeDictionary {
        public string bvhName;
        public string targetName;
    }

    // BVH to Unity
    private Quaternion fromEulerZXY(Vector3 euler) {
        return Quaternion.AngleAxis(euler.z, Vector3.forward) * Quaternion.AngleAxis(euler.x, Vector3.right) * Quaternion.AngleAxis(euler.y, Vector3.up);
    }

    private float wrapAngle(float a) {
        if (a > 180f) {
            return a - 360f;
        }
        if (a < -180f) {
            return 360f + a;
        }
        return a;
    }

    private string flexibleName(string name) {
        if (!flexibleBoneNames) {
            return name;
        }
        name = name.Replace(" ", "");
        name = name.Replace("_", "");
        name = name.ToLower();
        return name;
    }

    private Transform getBoneByName(string name, Transform transform, bool first) {
        string targetName = flexibleName(name);
        if (renamingMap.ContainsKey(targetName)) {
            targetName = flexibleName(renamingMap[targetName]);
        }
        if (first) { 
            if (flexibleName(transform.name) == targetName) {
                return transform;
            }
            if (nameMap.ContainsKey(targetName) && nameMap[targetName] == transform) {
                return transform;
            }
        }
        for (int i = 0; i < transform.childCount; i++) {
            Transform child = transform.GetChild(i);
            if (flexibleName(child.name) == targetName) {
                return child;
            }
            if (nameMap.ContainsKey(targetName) && nameMap[targetName] == child) {
                return child;
            }
        }
        throw new InvalidOperationException("Could not find bone \"" + name + "\" under bone \"" + transform.name + "\".");
    }

    private void getCurves(string path, BVHParser.BVHBone node, Transform bone, bool first) {
        bool posX = false;
        bool posY = false;
        bool posZ = false;
        bool rotX = false;
        bool rotY = false;
        bool rotZ = false;

        float[][] values = new float[6][];
        Keyframe[][] keyframes = new Keyframe[7][];
        string[] props = new string[7];
        Transform nodeTransform = getBoneByName(node.name, bone, first);

        if (path != prefix) {
            path += "/";
        }
        if (rootBone != targetAvatar.transform || !first) {
            path += nodeTransform.name;
        }

        // This needs to be changed to gather from all channels into two vector3, invert the coordinate system transformation and then make keyframes from it
        for (int channel = 0; channel < 6; channel++) {
            if (!node.channels[channel].enabled) {
                continue;
            }

            switch (channel) {
                case 0:
                    posX = true;
                    props[channel] = "localPosition.x";
                    break;
                case 1:
                    posY = true;
                    props[channel] = "localPosition.y";
                    break;
                case 2:
                    posZ = true;
                    props[channel] = "localPosition.z";
                    break;
                case 3:
                    rotX = true;
                    props[channel] = "localRotation.x";
                    break;
                case 4:
                    rotY = true;
                    props[channel] = "localRotation.y";
                    break;
                case 5:
                    rotZ = true;
                    props[channel] = "localRotation.z";
                    break;
                default:
                    channel = -1;
                    break;
            }
            if (channel == -1) {
                continue;
            }

            keyframes[channel] = new Keyframe[frames];
            values[channel] = node.channels[channel].values;
            if (rotX && rotY && rotZ && keyframes[6] == null) {
                keyframes[6] = new Keyframe[frames];
                props[6] = "localRotation.w";
            }
        }

        float time = 0f;
        if (posX && posY && posZ) {
            Vector3 offset;
            if (blender) {
                offset = new Vector3(-node.offsetX, node.offsetZ, -node.offsetY);
            } else {
                offset = new Vector3(-node.offsetX, node.offsetY, node.offsetZ);
            }
            for (int i = 0; i < frames; i++) {
                time += 1f / frameRate;
                keyframes[0][i].time = time;
                keyframes[1][i].time = time;
                keyframes[2][i].time = time;
                if (blender) {
                    keyframes[0][i].value = -values[0][i];
                    keyframes[1][i].value = values[2][i];
                    keyframes[2][i].value = -values[1][i];
                } else {
                    keyframes[0][i].value = -values[0][i];
                    keyframes[1][i].value = values[1][i];
                    keyframes[2][i].value = values[2][i];
                }
                if (first) {
                    Vector3 bvhPosition = bone.transform.parent.InverseTransformPoint(new Vector3(keyframes[0][i].value, keyframes[1][i].value, keyframes[2][i].value) + targetAvatar.transform.position + offset);
                    keyframes[0][i].value = bvhPosition.x * targetAvatar.transform.localScale.x;
                    keyframes[1][i].value = bvhPosition.y * targetAvatar.transform.localScale.y;
                    keyframes[2][i].value = bvhPosition.z * targetAvatar.transform.localScale.z;
                }
            }
            if (first) {
                clip.SetCurve(path, typeof(Transform), props[0], new AnimationCurve(keyframes[0]));
                clip.SetCurve(path, typeof(Transform), props[1], new AnimationCurve(keyframes[1]));
                clip.SetCurve(path, typeof(Transform), props[2], new AnimationCurve(keyframes[2]));
            } else {
                Debug.LogWarning("Position information on bones other than the root bone is currently not supported and has been ignored. If you exported this file from Blender, please tick the \"Root Translation Only\" option next time.");
            }
        }

        time = 0f;
        if (rotX && rotY && rotZ) {
            Quaternion oldRotation = bone.transform.rotation;
            for (int i = 0; i < frames; i++) {
                Vector3 eulerBVH = new Vector3(wrapAngle(values[3][i]), wrapAngle(values[4][i]), wrapAngle(values[5][i]));
                Quaternion rot = fromEulerZXY(eulerBVH);
                if (blender) {
                    keyframes[3][i].value = rot.x;
                    keyframes[4][i].value = -rot.z;
                    keyframes[5][i].value = rot.y;
                    keyframes[6][i].value = rot.w;
                    //rot2 = new Quaternion(rot.x, -rot.z, rot.y, rot.w);
                } else {
                    keyframes[3][i].value = rot.x;
                    keyframes[4][i].value = -rot.y;
                    keyframes[5][i].value = -rot.z;
                    keyframes[6][i].value = rot.w;
                    //rot2 = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);
                }
                if (first) {
                    bone.transform.rotation = new Quaternion(keyframes[3][i].value, keyframes[4][i].value, keyframes[5][i].value, keyframes[6][i].value);
                    keyframes[3][i].value = bone.transform.localRotation.x;
                    keyframes[4][i].value = bone.transform.localRotation.y;
                    keyframes[5][i].value = bone.transform.localRotation.z;
                    keyframes[6][i].value = bone.transform.localRotation.w;
                }
                /*Vector3 euler = rot2.eulerAngles;

                keyframes[3][i].value = wrapAngle(euler.x);
                keyframes[4][i].value = wrapAngle(euler.y);
                keyframes[5][i].value = wrapAngle(euler.z);*/

                time += 1f / frameRate;
                keyframes[3][i].time = time;
                keyframes[4][i].time = time;
                keyframes[5][i].time = time;
                keyframes[6][i].time = time;
            }
            bone.transform.rotation = oldRotation;
            clip.SetCurve(path, typeof(Transform), props[3], new AnimationCurve(keyframes[3]));
            clip.SetCurve(path, typeof(Transform), props[4], new AnimationCurve(keyframes[4]));
            clip.SetCurve(path, typeof(Transform), props[5], new AnimationCurve(keyframes[5]));
            clip.SetCurve(path, typeof(Transform), props[6], new AnimationCurve(keyframes[6]));
        }

        foreach (BVHParser.BVHBone child in node.children) {
            getCurves(path, child, nodeTransform, false);
        }
    }

    public static string getPathBetween(Transform target, Transform root, bool skipFirst, bool skipLast) {
        if (root == target) {
            if (skipLast) {
                return "";
            } else {
                return root.name;
            }
        }

        for (int i = 0; i < root.childCount; i++) {
            Transform child = root.GetChild(i);
            if (target.IsChildOf(child)) {
                if (skipFirst) {
                    return getPathBetween(target, child, false, skipLast);
                } else {
                    return root.name + "/" + getPathBetween(target, child, false, skipLast);
                }
            }
        }

        throw new InvalidOperationException("No path between transforms " + target.name + " and " + root.name + " found.");
    }

    private void getTargetAvatar() {
        if (targetAvatar == null) {
            targetAvatar = GetComponent<Animator>();
        }
        if (targetAvatar == null) {
            throw new InvalidOperationException("No target avatar set.");
        }

    }

	public void loadAnimation() {
        getTargetAvatar();

        if (bp == null) {
            throw new InvalidOperationException("No BVH file has been parsed.");
        }

        if (nameMap == null) {
            if (standardBoneNames) {
                Dictionary<Transform, string> boneMap;
                BVHRecorder.populateBoneMap(out boneMap, targetAvatar);
                nameMap = boneMap.ToDictionary(kp => flexibleName(kp.Value), kp => kp.Key);
            } else {
                nameMap = new Dictionary<string, Transform>();
            }
        }

        renamingMap = new Dictionary<string, string>();
        foreach (FakeDictionary entry in boneRenamingMap) {
            if (entry.bvhName != "" && entry.targetName != "") {
                renamingMap.Add(flexibleName(entry.bvhName), flexibleName(entry.targetName));
            }
        }

        Queue<Transform> transforms = new Queue<Transform>();
        transforms.Enqueue(targetAvatar.transform);
        string targetName = flexibleName(bp.root.name);
        if (renamingMap.ContainsKey(targetName)) {
            targetName = flexibleName(renamingMap[targetName]);
        }
        while (transforms.Any()) {
            Transform transform = transforms.Dequeue();
            if (flexibleName(transform.name) == targetName) {
                rootBone = transform;
                break;
            }
            if (nameMap.ContainsKey(targetName) && nameMap[targetName] == transform) {
                rootBone = transform;
                break;
            }
            for (int i = 0; i < transform.childCount; i++) {
                transforms.Enqueue(transform.GetChild(i));
            }
        }
        if (rootBone == null) {
            rootBone = BVHRecorder.getRootBone(targetAvatar);
            Debug.LogWarning("Using \"" + rootBone.name + "\" as the root bone.");
        }
        if (rootBone == null) {
            throw new InvalidOperationException("No root bone \"" + bp.root.name + "\" found." );
        }

        //frames = bp.frames;
        //clip = new AnimationClip();
        //clip.name = "BVHClip (" + (clipCount++) + ")";
        //if (clipName != "") {
        //    clip.name = clipName;
        //}
        //clip.legacy = true;
        //prefix = getPathBetween(rootBone, targetAvatar.transform, true, true);

        //Vector3 targetAvatarPosition = targetAvatar.transform.position;
        //Quaternion targetAvatarRotation = targetAvatar.transform.rotation;
        //targetAvatar.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
        //targetAvatar.transform.rotation = Quaternion.identity;
        
        


        //getCurves(prefix, bp.root, rootBone, true);
        
        //targetAvatar.transform.position = targetAvatarPosition;
        //targetAvatar.transform.rotation = targetAvatarRotation;
        
        //clip.EnsureQuaternionContinuity();
        //if (anim == null) {
        //    anim = targetAvatar.gameObject.GetComponent<Animation>();
        //    if (anim == null) {
        //        anim = targetAvatar.gameObject.AddComponent<Animation>();
        //    }
        //}
        //string path = "Assets/" + clip.name + " - " + clipCount + ".anim";
        //AssetDatabase.CreateAsset(clip, path);
        //AssetDatabase.SaveAssets();

        //anim.AddClip(clip, clip.name);
        //anim.clip = clip;
        //anim.playAutomatically = autoPlay;
        
        //if (autoPlay) {
        //    anim.Play(clip.name);
        //}
    }

    // This function doesn't call any Unity API functions and should be safe to call from another thread
    public void parse(string bvhData) {
        if (respectBVHTime) {
            bp = new BVHParser(bvhData);
            frameRate = 1f / bp.frameTime;
        } else {
            bp = new BVHParser(bvhData, 1f / frameRate);
        }
    }

    // This function doesn't call any Unity API functions and should be safe to call from another thread
    public void parseFile() {
        parse(File.ReadAllText(filename));
    }

    public void playAnimation() {
        if (bp == null) {
            throw new InvalidOperationException("No BVH file has been parsed.");
        }
        if (anim == null || clip == null) {
            loadAnimation();
        }
        anim.Play(clip.name);
    }

    public void stopAnimation() {
        if (clip != null) {
            if (anim.IsPlaying(clip.name)) {
                anim.Stop();
            }
        }
    }

    public void test()
    {
        int frameNum = bp.frames;


        pos.Clear();
        rot.Clear();
        BVHParser.BVHBone.BVHChannel[] channels = bp.root.channels;

        for (int i = 0; i < frameNum; i++)
        {
            Vector3 p, r;
            p = new Vector3(-channels[0].values[i], channels[2].values[i], -channels[1].values[i]);
            r = new Vector3(channels[3].values[i], channels[4].values[i], channels[5].values[i]);
            pos.Add(p);
            rot.Add(r);
            //Gizmos.DrawSphere(p, 3);
        }
        List<Vector3> reduced = CurvePreprocess.RdpReduce(pos, error);   // use the Ramer-Douglas-Pueker algorithm to remove unnecessary points
        Debug.Log(reduced.Count);
        curves.Clear();
        curves.AddRange(CurveFit.Fit(reduced, error2));            // fit the curves to those points
        createControlObj();

        DrawMutiCurve();
    }

    public void createControlObj()
    {
        // Create Control Point
        int maxControl = (curves.Count * 4 - (curves.Count - 1));
        if (cpts.Count < maxControl)
        {
            for (int i = cpts.Count; i < maxControl; i++)
            {
                cpts.Add(Instantiate(cptPrefab));
                cpts[cpts.Count - 1].transform.parent = GameObject.FindGameObjectWithTag("ControlPoints").transform;
            }
        }
        for (int i = 0; i < cpts.Count; i += 3)
        {
            int offset = 0;
            if (i / 4 > 0) offset = 1;

            int curveIndex = i == 0 ? 0 : (i - 1) / 3;
            cpts[i + 1 - offset].transform.position = curves[curveIndex].p1;
            cpts[i + 2 - offset].transform.position = curves[curveIndex].p2;
            cpts[i + 3 - offset].transform.position = curves[curveIndex].p3;
            if (i == 0)
            {
                cpts[i].transform.position = curves[curveIndex].p0;
                i++;
            }
        }

        // Create Line GameObject
        if (line.Count < curves.Count)
        {
            for (int i = line.Count; i < curves.Count; i++)
            {
                line.Add(new GameObject());
                line[line.Count - 1].transform.parent = GameObject.FindGameObjectWithTag("Lines").transform;
            }
        }
        else if (line.Count > curves.Count)
        {
            for (int i = line.Count - 1; i >= curves.Count; i--)
            {
                Destroy(line[i]);
                line.RemoveAt(i);
            }
        }
    }

    public void FitAnimation()
    {
        DrawMutiCurve();
        int frameNum = bp.frames;
        float maxLen = arcLen[arcLen.Count - 1];
        float eachLen = maxLen / frameNum;

        List<Vector3> framePoint = new List<Vector3>();
        List<Vector3> frameRot = new List<Vector3>();
        framePoint.Add(pts[0]);
        frameRot.Add(dpts[0]);
        for (int i = 1; i < pts.Count; i++)
        {
            if (arcLen[i] > (eachLen * framePoint.Count))
            {
                framePoint.Add(pts[i]);
                frameRot.Add(dpts[i]);
            }
        }
        if (framePoint.Count < frameNum)
        {
            for (int i = framePoint.Count; i < frameNum; i++)
            {
                framePoint.Add(pts[pts.Count - 1]);
                frameRot.Add(dpts[dpts.Count - 1]);
            }
        }

        Transform rootbone = rootBone;
        //for debug
        //int index = frameIndex == (frameNum - 1) ? (frameIndex - 1) : frameIndex;
        //rootbone.localPosition = new Vector3(framePoint[index].x, -framePoint[index].z, framePoint[index].y);
        //Vector3 norm = framePoint[index + 1] - framePoint[index];
        //norm = norm.normalized * 100;
        //norm = framePoint[index] + norm;
        //front.transform.localPosition = new Vector3(norm.x, -norm.z, norm.y);
        //rootbone.LookAt(front.transform.position);
        //rootbone.Rotate(new Vector3(0, 0, 1), -90);
        //rootbone.Rotate(new Vector3(1, 0, 0), 90);

        for (int i = 0; i < frameNum; i++)
        {
            int index = i == (frameNum - 1) ? (i - 1) : i;
            rootbone.localPosition = new Vector3(framePoint[index].x, -framePoint[index].z, framePoint[index].y);
            Vector3 norm = framePoint[index + 1] - framePoint[index];
            norm = norm.normalized * 100;
            norm = framePoint[index] + norm;
            front.transform.localPosition = new Vector3(norm.x, -norm.z, norm.y);
            rootbone.LookAt(front.transform.position);
            rootbone.Rotate(new Vector3(0, 0, 1), -90);
            rootbone.Rotate(new Vector3(1, 0, 0), 90);
            Vector3 Position = new Vector3(framePoint[i].x, -framePoint[i].z, framePoint[i].y);
            rootbone.Rotate(new Vector3(1, 0, 0), -90);
            Quaternion q = rootbone.transform.localRotation;
            Vector4 rot2 = new Vector4(q.x, -q.y, -q.z, q.w).normalized;
            Vector3 angles = eulerZXY(rot2);
            bp.root.channels[0].values[i] = -framePoint[i].x;
            bp.root.channels[1].values[i] = -framePoint[i].z;
            bp.root.channels[2].values[i] = framePoint[i].y;
            bp.root.channels[3].values[i] = wrapAngle(angles.x);
            bp.root.channels[4].values[i] = wrapAngle(angles.y);
            bp.root.channels[5].values[i] = wrapAngle(angles.z);
        }
        frames = bp.frames;
        clip = new AnimationClip();
        clip.name = "BVHClip (" + (clipCount++) + ")";
        if (clipName != "")
        {
            clip.name = clipName;
        }
        clip.legacy = true;
        prefix = getPathBetween(rootBone, targetAvatar.transform, true, true);

        Vector3 targetAvatarPosition = targetAvatar.transform.position;
        Quaternion targetAvatarRotation = targetAvatar.transform.rotation;
        targetAvatar.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
        targetAvatar.transform.rotation = Quaternion.identity;




        getCurves(prefix, bp.root, rootBone, true);

        targetAvatar.transform.position = targetAvatarPosition;
        targetAvatar.transform.rotation = targetAvatarRotation;

        clip.EnsureQuaternionContinuity();
        if (anim == null)
        {
            anim = targetAvatar.gameObject.GetComponent<Animation>();
            if (anim == null)
            {
                anim = targetAvatar.gameObject.AddComponent<Animation>();
            }
        }
        string path = "Assets/" + clip.name + " - " + clipCount + ".anim";
        AssetDatabase.CreateAsset(clip, path);
        AssetDatabase.SaveAssets();

        anim.AddClip(clip, clip.name);
        anim.clip = clip;
        anim.playAutomatically = autoPlay;

        if (autoPlay)
        {
            anim.Play(clip.name);
        }
    }
    // From: http://bediyap.com/programming/convert-quaternion-to-euler-rotations/
    Vector3 manualEuler(float a, float b, float c, float d, float e)
    {
        Vector3 euler = new Vector3();
        euler.z = Mathf.Atan2(a, b) * Mathf.Rad2Deg; // Z
        euler.x = Mathf.Asin(Mathf.Clamp(c, -1f, 1f)) * Mathf.Rad2Deg;     // Y
        euler.y = Mathf.Atan2(d, e) * Mathf.Rad2Deg; // X
        return euler;
    }

    // Unity to BVH
    Vector3 eulerZXY(Vector4 q)
    {
        return manualEuler(-2 * (q.x * q.y - q.w * q.z),
                      q.w * q.w - q.x * q.x + q.y * q.y - q.z * q.z,
                      2 * (q.y * q.z + q.w * q.x),
                     -2 * (q.x * q.z - q.w * q.y),
                      q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z); // ZXY
    }

    private void OnDrawGizmos()
    {
        //for (int i = 0; i < pos.Count; i++)
        //{
        //    Gizmos.DrawSphere(pos[i], 0.1f);
        //}
    }

    void DrawCurve(int c_numSamples, burningmime.curves.CubicBezier bezier, int index)
    {
        List<Vector3> drawpoint = new List<Vector3>();
        for (int i = 0; i < c_numSamples; ++i)
        {
            float percent = ((float)i) / (c_numSamples - 1);

            Vector3 drawpts = new Vector3();
            //drawpts = Hermitefunc(A, B, C, D, t);
            drawpts = bezier.Sample(percent);
            drawpoint.Add(drawpts);
            pts.Add(drawpts);
            dpts.Add(bezier.Tangent(percent));
            if (arcLen.Count == 0)
            {
                arcLen.Add(0);
            }
            else
            {
                arcLen.Add(arcLen[arcLen.Count - 1] + Vector3.Distance(pts[pts.Count - 1], pts[pts.Count - 2]));
            }
        }
        LineRenderer lineRenderer;
        if (line[index].GetComponent<LineRenderer>() == null)
            line[index].AddComponent<LineRenderer>();
        lineRenderer = line[index].GetComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Standard"));
        lineRenderer.sharedMaterial.SetColor("_Color", Color.red);
        lineRenderer.positionCount = drawpoint.Count;
        lineRenderer.SetPositions(drawpoint.ToArray());

    }

    public void DrawMutiCurve()
    {
        // Draw Spline
        arcLen.Clear();
        pts.Clear();
        dpts.Clear();
        for (int i = 0; i < curves.Count; i++)
        {
            DrawCurve(200, curves[i], i);
        }
    }

    public void reduceCurve()
    {
        if(curves.Count >= 2)
        {
            for (int i = 0; i < 3; i++)
            {
                Destroy(cpts[cpts.Count - 1]);
                cpts.RemoveAt(cpts.Count - 1);
            }

            Destroy(line[line.Count - 1]);
            line.RemoveAt(line.Count - 1);
            curves.RemoveAt(curves.Count - 1);

        }
    }

    public void AddCurve()
    {
        burningmime.curves.CubicBezier last = curves[curves.Count - 1];
        Vector3 dir = last.p3 - last.p2;
        dir *= 2;
        Vector3 p0 = last.p3 + dir;
        Vector3 p1 = last.p3 + dir * 1.5f;
        Vector3 p2 = last.p3 + dir * 2.25f;
        Vector3 p3 = last.p3 + dir * 3f;
        curves.Add(new burningmime.curves.CubicBezier(p0, p1, p2, p3));
        createControlObj();
    }

    void Start () {
        if (autoStart) {
            autoPlay = true;
            parseFile();
            loadAnimation();
        }
    }

    private void LateUpdate()
    {
        for (int i = 0, j = 0; i < cpts.Count; i += 3, j++)
        {
            int offset = 0;
            if (i / 4 > 0) offset = 1;

            int curveIndex = i == 0 ? 0 : (i - 1) / 3;
            Vector3 p1 = cpts[i + 1 - offset].transform.position;
            Vector3 p2 = cpts[i + 2 - offset].transform.position;
            Vector3 p3 = cpts[i + 3 - offset].transform.position;
            Vector3 p0 = Vector3.zero;
            if (i == 0)
            {
                p0 = cpts[i].transform.position;
                i++;
            }
            else
            {
                p0 = cpts[i - offset].transform.position;
            }
            curves[j] = new burningmime.curves.CubicBezier(p0, p1, p2, p3);
        }
        DrawMutiCurve();
    }
}
