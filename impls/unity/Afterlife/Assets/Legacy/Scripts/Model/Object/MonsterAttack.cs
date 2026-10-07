using UnityEngine;

public class MonsterAttack : StateMachineBehaviour
{
    Afterlife.View.Monster monster;
    float progress;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (monster == null)
        {
            monster = animator.GetComponent<Afterlife.View.Monster>();
        }

        monster.StateName = "Attack";

        var direction = monster.targetLocation.x - monster.transform.position.x;
        if (direction != 0 && (direction < 0) != monster.BodySpriteRenderer.flipX)
        {
            monster.SetDirection(direction > 0 ? Afterlife.View.ObjectDirection.Right : Afterlife.View.ObjectDirection.Left);
        }

        progress = 0f;
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var hasTarget = monster.SqrDistanceToTarget(out var sqrDistance);
        if (!hasTarget)
        {
            monster.StopAttack();
            return;
        }
        if (!monster.IsInAttackRange(sqrDistance))
        {
            monster.StopAttack();
            return;
        }

        progress += Time.deltaTime * monster.AttackSpeed;
        if (progress >= 1f)
        {
            progress = 0f;
        }
    }
}
