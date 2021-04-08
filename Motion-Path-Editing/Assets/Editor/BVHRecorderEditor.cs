using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(BVHRecorder))]
public class BVHRecorderEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        BVHRecorder bvhRecorder = (BVHRecorder)target;

        if (GUILayout.Button("Get Hierachy"))
        {
            bvhRecorder.genHierarchy();
            Debug.Log("Get Hierachy done.");
        }

        if (GUILayout.Button("Detect bones")) {
            bvhRecorder.getBones();
            Debug.Log("Bone detection done.");
        }
        
        if (GUILayout.Button("Remove empty entries from bone list")) {
            bvhRecorder.cleanupBones();
            Debug.Log("Cleaned up bones.");
        }

        if (GUILayout.Button("Clear recorded motion data")) {
            bvhRecorder.clearCapture();
            Debug.Log("Cleared motion data.");
        }

        if (GUILayout.Button("Build Skeleton"))
        {
            bvhRecorder.buildSkeleton();
            Debug.Log("Build Skeleton");
        }

        if (GUILayout.Button("Save motion to BVH file")) {
            try {
                //bvhRecorder.genHierarchy();
                bvhRecorder.saveBVH();
            } catch (Exception ex) {
                Debug.LogError("An error has occurred while saving the BVH file: " + ex);
            }
        }
    }
}
