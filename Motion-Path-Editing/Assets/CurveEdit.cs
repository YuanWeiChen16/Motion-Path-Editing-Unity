using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace burningmime.curves
{

    public class CurveEdit : MonoBehaviour
    {
        List<Vector3> cubes = new List<Vector3>();
        // Start is called before the first frame update
        void Start()
        {
            Transform[] trans = GetComponentsInChildren<Transform>();
            
            for (int i = 0; i < trans.Length; i++)
            {
                if (trans[i].gameObject.GetComponent<MeshRenderer>() != null)
                {
                    cubes.Add(trans[i].transform.position);
                }
            }
            // List<Vector3> reduced =  CurvePreprocess.RdpReduce(cubes, 30f);   // use the Ramer-Douglas-Pueker algorithm to remove unnecessary points
            CubicBezier[] curves = CurveFit.Fit(cubes, 901);
            DrawCurve(100, curves[0]);
        }

        // Update is called once per frame
        void Update()
        {

        }

        void DrawCurve(int c_numSamples, CubicBezier bezier)
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
            lineRenderer = this.GetComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Standard"));
            lineRenderer.sharedMaterial.SetColor("_Color", Color.red);
            lineRenderer.positionCount = drawpoint.Count;
            lineRenderer.SetPositions(drawpoint.ToArray());

        }
    }
}

