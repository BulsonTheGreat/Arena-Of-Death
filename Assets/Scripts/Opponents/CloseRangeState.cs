using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloseRangeState : StateMachineBehaviour
{
    ThirdPersonController player;
    public int meleeRange;
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<ThirdPersonController>();
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.transform.LookAt(player.transform.position);
        float distance = Vector3.Distance(player.transform.position, animator.transform.position);
        if (distance > meleeRange)
        {
            animator.SetBool("isClose", false);
        }
        
        if (player.hp <= 0)
        {
            animator.SetBool("isClose", false);
        }
    }
}
