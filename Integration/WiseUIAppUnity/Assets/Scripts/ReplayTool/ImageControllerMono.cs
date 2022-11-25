using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


#if UNITY_EDITOR
[ExecuteInEditMode]
#endif

public class ImageControllerMono : MonoBehaviour
{
    const float size_gizmo = 1f;

    bool showPointCloud = true;

    public GameObject cam;
    [HideInInspector]
    //public bool isSelected;
    public int selState = 0; //0 ; unsel, 1 ; only, 2; additionally

    void Awake()
    {
        cam = transform.Find("Camera").gameObject;
        if (cam == null)
            Debug.LogError("This gameobject must include \"Camera\" as child object.");
    }

    //선택, zoom level에 따른 Gizmo 색상 결정.
    void OnDrawGizmos()
    {
        switch(selState)
        {
            case 0://none seleced.
                Gizmos.color = new Color(0f, 0f, 0f, 0.2f);
                break;

            case 1://single selected.
                Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
                break;

            case 2: //multi selected.
                Gizmos.color = new Color(1f, 1f, 0f, 0.8f);
                break;

        }

        Gizmos.DrawSphere(transform.position, transform.lossyScale.x * size_gizmo);
        Gizmos.color = new Color(1f, 0f, 1f, 1f);
        //Gizmos.DrawLine(transform.position, transform.position + -transform.up);

    }

    public void OnUnselected()
    {
        selState = 0;
        GetComponent<MeshRenderer>().enabled = false;
        cam.SetActive(false);
    }
    public void OnSelectedOnly()
    {
        selState = 1;
        GetComponent<MeshRenderer>().enabled = true;
        cam.SetActive(true);
    }
    public void OnSelectedAdditionaly()
    {
        selState = 2;
        GetComponent<MeshRenderer>().enabled = false;
        cam.SetActive(false);
    }

    void OnGUI()
    {
        if (Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint)
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this); // this is important, if omitted, "Mouse down" will not be display
#endif
        }

        if (selState == 1)
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1) //right mouse down
            {
                if (showPointCloud)
                {
                    cam.GetComponent<Camera>().cullingMask = 1;
                }
                else
                {
                    cam.GetComponent<Camera>().cullingMask |= 1 << LayerMask.NameToLayer("PointCloud");
                }
                showPointCloud = !showPointCloud;
            }
        }
    }


}
