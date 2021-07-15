using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Fire Breath Skill", menuName = "ScriptableObject/Skills/Fire Breath")]
public class FireBreathSkill : SkillScriptableObject
{
    public float Duration = 3;
    public float TickRate = 0.5f;
    public float Range = 3;
    public PoolableObject Prefab;

    public override SkillScriptableObject ScaleUpForLevel(ScalingScriptableObject Scaling, int Level)
    {
        FireBreathSkill scaledSkill = CreateInstance<FireBreathSkill>();

        ScaleUpBaseValuesForLevel(scaledSkill, Scaling, Level);
        scaledSkill.Duration = Duration;
        scaledSkill.TickRate = TickRate;
        scaledSkill.Prefab = Prefab;

        return scaledSkill;
    }

    public override bool CanUseSkill(Enemy Enemy, Player Player, int Level)
    {
        return base.CanUseSkill(Enemy, Player, Level) && Vector3.Distance(Enemy.transform.position, Player.transform.position) <= Range;
    }

    public override void UseSkill(Enemy Enemy, Player Player)
    {
        base.UseSkill(Enemy, Player);
        Enemy.StartCoroutine(BreatheFire(Enemy, Player));
    }

    private IEnumerator BreatheFire(Enemy Enemy, Player Player)
    {
        Enemy.Animator.SetBool(EnemyMovement.IsWalking, false);

        DisableEnemyMovement(Enemy);
        Enemy.Movement.State = EnemyState.UsingAbility;

        for (float time = 0; time < 1; time += Time.deltaTime * 5)
        {
            Enemy.transform.rotation = Quaternion.Slerp(Enemy.transform.rotation,
                Quaternion.LookRotation(Player.transform.position - Enemy.transform.position),
                time);
            yield return null;
        }

        ObjectPool pool = ObjectPool.CreateInstance(Prefab, 5);

        PoolableObject instance = pool.GetObject();

        if (instance != null)
        {
            instance.transform.SetParent(Enemy.Agent.transform, false);
            instance.transform.localPosition = new Vector3(0, 1, 0);
            AreaDamage areaDamage = instance.GetComponentInChildren<AreaDamage>();

            areaDamage.Damage = Damage;
            areaDamage.TickRate = TickRate;
        }

        for (float time = 0; time < Duration; time += Time.deltaTime)
        {
            Enemy.transform.LookAt(Player.transform.position);
            yield return null;
        }

        UseTime = Time.time;
        instance.gameObject.SetActive(false);

        EnableEnemyMovement(Enemy);
        Enemy.Movement.State = EnemyState.Chase;

        IsActivating = false;
    }
}
