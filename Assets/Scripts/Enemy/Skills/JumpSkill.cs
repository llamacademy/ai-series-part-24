using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "Jump Skill", menuName = "ScriptableObject/Skills/Jump")]
public class JumpSkill : SkillScriptableObject
{
    public float MinJumpDistance = 1.5f;
    public float MaxJumpDistance = 5f;
    public AnimationCurve HeightCurve;
    public float JumpSpeed = 1;

    public override SkillScriptableObject ScaleUpForLevel(ScalingScriptableObject Scaling, int Level)
    {
        JumpSkill Instance = CreateInstance<JumpSkill>();

        ScaleUpBaseValuesForLevel(Instance, Scaling, Level);
        Instance.MinJumpDistance = MinJumpDistance;
        Instance.MaxJumpDistance = MaxJumpDistance;
        Instance.HeightCurve = HeightCurve;
        Instance.JumpSpeed = JumpSpeed;

        return Instance;
    }

    public override bool CanUseSkill(Enemy Enemy, Player Player, int Level)
    {
        if (base.CanUseSkill(Enemy, Player, Level))
        {
            float distance = Vector3.Distance(Enemy.transform.position, Player.transform.position);

            return distance >= MinJumpDistance
                && distance <= MaxJumpDistance;
        }

        return false;
    }

    public override void UseSkill(Enemy Enemy, Player Player)
    {
        base.UseSkill(Enemy, Player);
        Enemy.StartCoroutine(Jump(Enemy, Player));
    }

    private IEnumerator Jump(Enemy Enemy, Player Player)
    {
        DisableEnemyMovement(Enemy);
        Enemy.Movement.State = EnemyState.UsingAbility;

        Vector3 startingPosition = Enemy.transform.position;
        Enemy.Animator.SetTrigger(EnemyMovement.Jump);

        for (float time = 0; time < 1; time += Time.deltaTime * JumpSpeed)
        {
            Enemy.transform.position = Vector3.Lerp(startingPosition, Player.transform.position, time)
                + Vector3.up * HeightCurve.Evaluate(time);
            Enemy.transform.rotation = Quaternion.Slerp(Enemy.transform.rotation,
                Quaternion.LookRotation(Player.transform.position - Enemy.transform.position),
                time);

            yield return null;
        }
        Enemy.Animator.SetTrigger(EnemyMovement.Landed);

        UseTime = Time.time;

        EnableEnemyMovement(Enemy);

        if (NavMesh.SamplePosition(Player.transform.position, out NavMeshHit hit, 1f, Enemy.Agent.areaMask))
        {
            Enemy.Agent.Warp(hit.position);
            Enemy.Movement.State = EnemyState.Chase;
        }

        IsActivating = false;
    }
}
