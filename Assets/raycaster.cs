using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

[ExecuteAlways]
public class raycaster : MonoBehaviour
{

    public GameObject _reticle;
    public GameObject _lineObj;
    public GameObject _indicator;
    public List<GameObject> _lines = new List<GameObject>();
    public List<GameObject> _indicators = new List<GameObject>();


    public int lineRez = 100;
    public int nLines = 100;

    [ContextMenu("clear")]
    void clear()
    {
        for (int i = 0; i < _lines.Count; i++)
        {
            DestroyImmediate(_lines[i]);
        }

        _lines.Clear();

        for (int i = 0; i < _indicators.Count; i++)
        {
            DestroyImmediate(_indicators[i]);
        }

        _indicators.Clear();
    }

    Vector3 computePos(int i, int j)
    {
        var pos = new Vector3(
                   transform.localScale.x * ( (float)i / (float)lineRez - 0.5f),
                   10,
                   transform.localScale.z * ((float)j / (float)nLines - 0.5f));

        return  pos + transform.position;
    }

    [ContextMenu("castLine")]
    void cast()
    {

        clear();


        var hits = new RaycastHit[4];


        for (int j = 0; j < nLines; j++ )
        {

            var l = GameObject.Instantiate(_lineObj);
            var line = l.GetComponent<LineRenderer>();
            var intersects = new List<Vector3>();

            for (int i = 0; i < lineRez; i++)
            {
                var origin = computePos(i, j);

                var direction = new Vector3(0, -1, 0);

              //  var ind = Instantiate(_indicator);
              //  ind.transform.position = origin;
              //  _indicators.Add(ind);

                var nHits = Physics.RaycastNonAlloc(new Ray(origin, direction), hits, 1000);

                if (nHits > 0)
                {
                    int closest = 0;
                    float closestDist = 999999;
                    for (int k = 0; k < nHits;k++)
                    {
                        if ( hits[k].distance < closestDist)
                        {
                            closestDist = hits[k].distance;
                            closest = k;
                        }
                    }
                    intersects.Add(hits[closest].point);
                }
            }

            line.positionCount = intersects.Count;
            line.SetPositions(intersects.ToArray());

            _lines.Add(l);
        }
    }

    RaycastHit[] mouseHits = new RaycastHit[2];

    private string pointString(Vector3 w)
    {
        var p = Camera.main.WorldToScreenPoint(w);
        var s = 0.3;
        return s * p.x / Screen.width + " " + s * (Screen.height - p.y) / Screen.width;
    }

    private string createPathString(List<Vector3> l)
    {
        var result = String.Empty;
        result += "<path fill=\"none\" stroke=\"black\" stroke-width=\"0.01\" d=\"";

        result += "M " + pointString(l[0]) + " ";

        for (int i = 1; i < l.Count; i ++)
        {
            result += "L " + pointString(l[i]) + " ";
        }

        result += " \" ></path>";
        return result;
    }

    bool visibleFromCamera(Vector3 p)
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(p);
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit))
        {
            return (hit.point - p).sqrMagnitude < 0.001f;
        } 
        else
        {
            return false;
        }
    }

    List<List<Vector3>> splitSegments(LineRenderer l)
    {
        var result = new List<List<Vector3>>();

        int lineIndex = -1;

        for (int i = 0; i < l.positionCount; i ++)
        {
            var p = l.GetPosition(i);

            if (visibleFromCamera(p))
            {
                if ( lineIndex == -1)
                {
                    lineIndex = result.Count;
                    result.Add(new List<Vector3>());
                }

                result[lineIndex].Add(p);
            } else
            {
                lineIndex = -1;
            }
        }

        return result;
    }

    [ContextMenu("SVG")]
    private void writeSVG()
    {
        //var fullPath = "E:\\lidar\\isolines\\" + DateTime.Now.ToString().Replace("/", "-") + ".svg";
        string fullPath = "Assets\\" +  DateTime.Now.ToString("dd-MM-yy.h.m.s") + ".svg";

        using (StreamWriter writer = new StreamWriter(fullPath))
        {
            writer.WriteLine("<svg viewBox=\" -1 -1 2 2\" version=\"1.1\" aria-hidden=\"true\">");

            for(int i = 0; i < _lines.Count; i ++)
            {
                var segments = splitSegments(_lines[i].GetComponent<LineRenderer>());
                foreach (var segment in segments)
                {
                    writer.WriteLine(createPathString(segment));
                }
            }
            writer.WriteLine("</svg>");
        }
    }


    private void Update()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var nHits = Physics.RaycastNonAlloc(ray, mouseHits, 100);
        
        if (nHits > 0)
        {
           _reticle.transform.position = mouseHits[0].point;
        }

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
