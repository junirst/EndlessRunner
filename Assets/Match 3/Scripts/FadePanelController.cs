using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadePanelController : MonoBehaviour
{
    public Animator panelAnim;
    public Animator gameInforAnim;


    public void Okay()
    {
        if (panelAnim != null && gameInforAnim != null)
        {
            panelAnim.SetBool("Out", true);
            gameInforAnim.SetBool("Out", true);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
