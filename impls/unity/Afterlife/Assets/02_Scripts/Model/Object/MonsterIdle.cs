using UnityEngine;

public class MonsterIdle : StateMachineBehaviour
{
    Afterlife.View.Monster monster;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (monster == null)
        {
            monster = animator.GetComponent<Afterlife.View.Monster>();
        }

        monster.StateName = "Idle";
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (Random.value < 0.5f)
        {
            monster.StartPatrol();
        }
    }
}
