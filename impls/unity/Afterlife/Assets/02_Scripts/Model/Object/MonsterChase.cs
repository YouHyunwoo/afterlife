using UnityEngine;

public class MonsterChase : StateMachineBehaviour
{
    Afterlife.View.Monster monster;
    float progress;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (monster == null)
        {
            monster = animator.GetComponent<Afterlife.View.Monster>();
        }

        monster.StateName = "Chase";

        progress = 0f;
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var hasTarget = monster.SqrDistanceToTarget(out var sqrDistance);
        if (!hasTarget)
        {
            monster.StopChase();
            return;
        }
        if (!monster.IsInDetectingRange(sqrDistance))
        {
            monster.StopChase();
            return;
        }
        if (monster.IsInAttackRange(sqrDistance))
        {
            monster.StartAttack();
            return;
        }

        progress += Time.deltaTime * monster.MovementSpeed;
        if (progress >= 1f)
        {
            progress = 0f;
            var reachable = monster.ChaseStep();
            if (!reachable)
            {
                monster.StopChase();
            }
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        monster.StopChase();
    }
}
