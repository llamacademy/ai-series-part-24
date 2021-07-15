using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : PoolableObject, IDamageable
{
    public AttackRadius AttackRadius;
    public Player Player;
    public int Level;
    public Animator Animator;
    public EnemyMovement Movement;
    public NavMeshAgent Agent;
    public int Health = 100;
    public SkillScriptableObject[] Skills;
    public delegate void DeathEvent(Enemy enemy);
    public DeathEvent OnDie;

    private Coroutine LookCoroutine;
    public const string ATTACK_TRIGGER = "Attack";

    private void Awake()
    {
        AttackRadius.OnAttack += OnAttack;
    }

    private void Update()
    {
        for (int i = 0; i < Skills.Length; i++)
        {
            if (Skills[i].CanUseSkill(this, Player, Level))
            {
                Skills[i].UseSkill(this, Player);
            }
        }
    }

    private void OnAttack(IDamageable Target)
    {
        Animator.SetTrigger(ATTACK_TRIGGER);

        if (LookCoroutine != null)
        {
            StopCoroutine(LookCoroutine);
        }

        LookCoroutine = StartCoroutine(LookAt(Target.GetTransform()));
    }

    private IEnumerator LookAt(Transform Target)
    {
        Quaternion lookRotation = Quaternion.LookRotation(Target.position - transform.position);
        float time = 0;

        while (time < 1)
        {
            Quaternion targetRotation = Quaternion.Slerp(transform.rotation, lookRotation, time);
            transform.rotation = Quaternion.Euler(transform.rotation.x, targetRotation.eulerAngles.y, transform.rotation.z);

            time += Time.deltaTime * 2;
            yield return null;
        }

        transform.rotation = lookRotation;
    }

    public override void OnDisable()
    {
        base.OnDisable();

        Agent.enabled = false;
        OnDie = null;
    }

    public void TakeDamage(int Damage)
    {
        Health -= Damage;

        if (Health <= 0)
        {
            OnDie?.Invoke(this);
            gameObject.SetActive(false);
        }
    }

    public Transform GetTransform()
    {
        return transform;
    }
}
