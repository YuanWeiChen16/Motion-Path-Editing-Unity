using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class hermitesplineline : MonoBehaviour
{
    public int Datasize = 4;
    public float tangLineWidth = 0.5f;
    public List<GameObject> points;
    public List<GameObject> tang;
    public GameObject t0, t1 ,t2;
    public float c_numSamples = 100;
    public List<Vector3> drawpoint;
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < Datasize; i++)
        {
            points.Add(GameObject.CreatePrimitive(PrimitiveType.Cube));
            points[i].transform.position = new Vector3(i*10, 0, i*10);
            points[i].gameObject.name = "P" + i.ToString();
        }

        for (int i = 0; i < Datasize; i++)
        {
            tang.Add(GameObject.CreatePrimitive(PrimitiveType.Cube));
            tang[i].gameObject.name = "t" + i.ToString();
            tang[i].transform.position = Vector3.zero;
            tang[i].AddComponent<LineRenderer>();
            tang[i].GetComponent<LineRenderer>().startWidth = tangLineWidth;
            tang[i].GetComponent<LineRenderer>().endWidth = tangLineWidth;
        }

       // t0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
     //   t1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
      //  t2 = GameObject.CreatePrimitive(PrimitiveType.Cube);

       // points[0].gameObject.name = "P0";
       // points[1].gameObject.name = "P1";
      //  t0.gameObject.name = "t0";
     //   t1.gameObject.name = "t1";
     //   t2.gameObject.name = "t2";


      //  points[0].transform.position = new Vector3(0, 0, 0);
      //  points[1].transform.position = new Vector3(10, 0, 10);
       // t0.transform.position = new Vector3(0, 0, 0);
      //  t1.transform.position = new Vector3(0, 0, 0);
      //  t2.transform.position = new Vector3(0, 0, 0);


       // t0.AddComponent<LineRenderer>();
       // t1.AddComponent<LineRenderer>();
      //  t2.AddComponent<LineRenderer>();

    }

    // Update is called once per frame
    void Update()
    {

        DrawSplineline(points, tang);
        /*
        for (int i = 0; i < c_numSamples; ++i)
        {
            
            float percent = ((float)i) / (c_numSamples - 1);
            float x = (points.Count- 1) * percent;

            int index = (int)x;
            float t = x - Mathf.Floor(x);
            GameObject A = GetIndexClamped(points, index );
            GameObject B = GetIndexClamped(points, index + 1);
            //GameObject C = GetIndexClamped(points, index + 2);
            // GameObject D = GetIndexClamped(points, index + 3);

            GameObject C = t0;
            GameObject D =t1;

            Vector3 drawpts = new Vector3();
            //drawpts = Hermitefunc(A, B, C, D, t);
            drawpts = Hermitefunc(A.transform.position, B.transform.position, C.transform.position, D.transform.position, t);
            drawpoint.Add(drawpts);
        }
        LineRenderer lineRenderer;
        lineRenderer = this.GetComponent<LineRenderer>();
        lineRenderer.material =  new Material(Shader.Find("Standard"));
        lineRenderer.sharedMaterial.SetColor("_Color", Color.red);
        lineRenderer.positionCount = drawpoint.Count;
        lineRenderer.SetPositions(drawpoint.ToArray());

        LineRenderer t0line = t0.GetComponent<LineRenderer>();
        LineRenderer t1line = t1.GetComponent<LineRenderer>();

        t0line.positionCount = 2;
        t1line.positionCount = 2;
        
        t0line.material = new Material(Shader.Find("Standard"));
        t1line.material = new Material(Shader.Find("Standard"));

        t0line.sharedMaterial.SetColor("_Color", Color.blue);
        t1line.sharedMaterial.SetColor("_Color", Color.green);

        t0line.SetPosition(0, t0.transform.position);
        t0line.SetPosition(1, points[0].transform.position);

        t1line.SetPosition(0, t1.transform.position);
        t1line.SetPosition(1, points[1].transform.position);

        drawpoint.Clear();
        */



    }


    public void  DrawSplineline(List<GameObject> points , List<GameObject> tang)
    {
        for (int i = 0; i < c_numSamples; ++i)
        {

            float percent = ((float)i) / (c_numSamples - 1);
            float x = (points.Count - 1) * percent;

            int index = (int)x;
            float t = x - Mathf.Floor(x);
            GameObject A = GetIndexClamped(points, index);
            GameObject B = GetIndexClamped(points, index + 1);
            //GameObject C = GetIndexClamped(points, index + 2);
            // GameObject D = GetIndexClamped(points, index + 3);

            GameObject C = GetIndexClamped(tang, index);
            GameObject D = GetIndexClamped(tang, index+1);

            Vector3 drawpts = new Vector3();
            //drawpts = Hermitefunc(A, B, C, D, t);
            drawpts = Hermitefunc(A.transform.position, B.transform.position, C.transform.position, D.transform.position, t);
            drawpoint.Add(drawpts);
        }
        LineRenderer lineRenderer;
        lineRenderer = this.GetComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Standard"));
        lineRenderer.sharedMaterial.SetColor("_Color", Color.red);
        lineRenderer.positionCount = drawpoint.Count;
        lineRenderer.SetPositions(drawpoint.ToArray());


        List<LineRenderer> tline  = new List<LineRenderer>();

        for(int i = 0; i < tang.Count; i++) 
        {
            tline.Add(tang[i].GetComponent<LineRenderer>());
            tline[i].positionCount = 2;
            tline[i].material = new Material(Shader.Find("Standard"));
            if (i % 2 == 0)
            {
                tline[i].sharedMaterial.SetColor("_Color", Color.blue);
            }
            else 
            {
                tline[i].sharedMaterial.SetColor("_Color", Color.green);
            }
        }

        for (int i = 0; i < points.Count; i++) 
        {
            tline[i].SetPosition(0, tang[i].transform.position);
            tline[i].SetPosition(1, points[i].transform.position);
        }
        
        drawpoint.Clear();
        /*
        LineRenderer t0line = t0.GetComponent<LineRenderer>();
        LineRenderer t1line = t1.GetComponent<LineRenderer>();

        t0line.positionCount = 2;
        t1line.positionCount = 2;

        t0line.material = new Material(Shader.Find("Standard"));
        t1line.material = new Material(Shader.Find("Standard"));

        t0line.sharedMaterial.SetColor("_Color", Color.blue);
        t1line.sharedMaterial.SetColor("_Color", Color.green);

        t0line.SetPosition(0, t0.transform.position);
        t0line.SetPosition(1, points[0].transform.position);

        t1line.SetPosition(0, t1.transform.position);
        t1line.SetPosition(1, points[1].transform.position);*/
    }


    public GameObject GetIndexClamped(List<GameObject> points, int index)
{
        if (index < 0)
            return points[0];
        else if (index >= (points.Count))
            return points[points.Count - 1];
            return points[index];
}



public Vector3 Hermitefunc( Vector3 P0 , Vector3 P1, Vector3 P2 , Vector3 P3, float t) 
    {
        /* Vector3 a = -P0 / 2.0f + (3.0f * P1) / 2.0f - (3.0f * P2) / 2.0f + P3 / 2.0f;
         Vector3 b= P0 - (5.0f * P1) / 2.0f + 2.0f * P2 - P3 / 2.0f;
         Vector3 c = -P0 / 2.0f + P2 / 2.0f;
         Vector3 d = P1;*/
        Vector3 a = (1 - 3 * Mathf.Pow(t, 2) + 2 * Mathf.Pow(t, 3)) * P0;
        Vector3 b = Mathf.Pow(t, 2) * (3 - 2 * t) * P1;
        Vector3 c = t * Mathf.Pow((t - 1), 2) * P2;
        Vector3 d = Mathf.Pow(t, 2) * (t - 1) * P3;
        //return a * Mathf.Pow(t, 3) + b * Mathf.Pow(t, 2) + c * t + d;
        return a + b + c + d;
    }


}
