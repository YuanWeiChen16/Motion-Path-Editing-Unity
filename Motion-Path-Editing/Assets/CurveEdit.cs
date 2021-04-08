using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace burningmime.curves
{

    public class CurveEdit : MonoBehaviour
    {
        List<Vector3> cubes = new List<Vector3>();
        List<CubicBezier> curves = new List<CubicBezier>();
        List<GameObject> cpt = new List<GameObject>();
        public GameObject controlPointObj;
        public float error;
        // Start is called before the first frame update
        void Start()
        {
            //Debug.Log(arcL.Count);
            //Debug.Log(pts.Count);
            
        }

        // Update is called once per frame
        void Update()
        {
            Transform[] trans = GetComponentsInChildren<Transform>();
            cubes.Clear();
            for (int i = 0; i < trans.Length; i++)
            {
                if (trans[i].gameObject.GetComponent<MeshRenderer>() != null)
                {
                    cubes.Add(trans[i].transform.position);
                }
            }
            // List<Vector3> reduced =  CurvePreprocess.RdpReduce(cubes, 30f);   // use the Ramer-Douglas-Pueker algorithm to remove unnecessary points
            List<float> arcL = new List<float>();
            List<Vector3> pts = new List<Vector3>();
            curves.Clear();
            curves.AddRange(CurveFit.Fit(cubes, error, ref pts, ref arcL));
            Debug.Log(curves.Count);
            if (cpt.Count < curves.Count)
            {
                for(int i=cpt.Count;i<curves.Count;i++)
                {
                    cpt.Add(Instantiate(controlPointObj));
                }
            }
            else if (cpt.Count > curves.Count)
            {
                for(int i=cpt.Count - 1;i>=curves.Count;i--)
                {
                    Destroy(cpt[i]);
                    cpt.RemoveAt(i);
                }
            }
            for (int i=0;i<curves.Count;i++)
            { 
                DrawCurve(100, curves[i], i);
            }
        }

        void DrawCurve(int c_numSamples, CubicBezier bezier, int index)
        {
            List<Vector3> drawpoint = new List<Vector3>();
            for (int i = 0; i < c_numSamples; ++i)
            {

                float percent = ((float)i) / (c_numSamples - 1);

                Vector3 drawpts = new Vector3();
                //drawpts = Hermitefunc(A, B, C, D, t);
                drawpts = bezier.Sample(percent);
                drawpoint.Add(drawpts);
            }
            LineRenderer lineRenderer;
            if (cpt[index].GetComponent<LineRenderer>() == null)
                cpt[index].AddComponent<LineRenderer>();
            lineRenderer = cpt[index].GetComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Standard"));
            lineRenderer.sharedMaterial.SetColor("_Color", Color.red);
            lineRenderer.positionCount = drawpoint.Count;
            lineRenderer.SetPositions(drawpoint.ToArray());

        }
    }
}

