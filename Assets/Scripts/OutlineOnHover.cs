using UnityEngine;

public class OutlineOnHover : MonoBehaviour
{
    private Renderer rend;
    private Material mat;

    void Start()
    {
        rend = GetComponent<Renderer>();
        mat = rend.material;
        mat.SetFloat("_OutlineWidth", 0f);
    }

    public void OnHoverEnter()
    {
        mat.SetFloat("_OutlineWidth", 0.0001f);
    }

    public void OnHoverExit()
    {
        mat.SetFloat("_OutlineWidth", 0f);
    }
}