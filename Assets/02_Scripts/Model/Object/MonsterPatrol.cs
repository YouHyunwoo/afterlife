using UnityEngine;

public class MonsterPatrol : StateMachineBehaviour
{
    Afterlife.View.Monster monster;
    float progress;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (monster == null)
        {
            monster = animator.GetComponent<Afterlife.View.Monster>();
        }

        monster.StateName = "Patrol";

        progress = 0f;
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var isFound = monster.FindNearestTarget(out var targetTransform);
        if (isFound)
        {
            monster.targetTransform = targetTransform;
            monster.StartChase();
            return;
        }

        var isArrived = monster.IsArrived();
        if (isArrived)
        {
            monster.StopPatrol();
            return;
        }

        progress += Time.deltaTime * monster.MovementSpeed;
        if (progress >= 1f)
        {
            progress = 0f;
            var reachable = monster.PatrolStep();
            if (!reachable)
            {
                monster.StopPatrol();
            }
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        monster.StopPatrol();
    }
}
