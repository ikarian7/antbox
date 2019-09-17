using UnityEngine;

public class AntBox : MonoBehaviour
{
    [SerializeField] LayerMask antLayerMask;
    [SerializeField] Camera cam;
    [SerializeField] float clickRange = 2f;
    int antLayer;

    void Awake()
    {
        antLayer = antLayerMask.value;
    }

    void Update()
    {
        if(Input.GetMouseButtonDown(0)) {
            Vector3 mouseLoc = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -cam.transform.position.z)); // This camera -z thing only works if the camera is not rotated
            Collider2D[] ants = Physics2D.OverlapCircleAll(mouseLoc, clickRange, antLayer);
            foreach(Collider2D ant in ants) {
                ant.GetComponent<Ant>().Clicked();
            }
        }
    }
}
