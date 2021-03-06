﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Roundbeargames
{
    public class DamageDetector : MonoBehaviour
    {
        CharacterControl control;

        [SerializeField]
        private float hp;

        [SerializeField]
        List<RuntimeAnimatorController> HitReactionList = new List<RuntimeAnimatorController>();
        
        private void Awake()
        {
            control = GetComponent<CharacterControl>();
        }

        private void Update()
        {
            if (AttackManager.Instance.CurrentAttacks.Count > 0)
            {
                CheckAttack();
            }
        }

        private bool AttackIsValid(AttackInfo info)
        {
            if (info == null)
            {
                return false;
            }

            if (!info.isRegisterd)
            {
                return false;
            }

            if (info.isFinished)
            {
                return false;
            }

            if (info.CurrentHits >= info.MaxHits)
            {
                return false;
            }

            if (info.Attacker == control)
            {
                return false;
            }

            if (info.MustFaceAttacker)
            {
                Vector3 vec = this.transform.position - info.Attacker.transform.position;
                if (vec.z * info.Attacker.transform.forward.z < 0f)
                {
                    return false;
                }
            }

            if (info.RegisteredTargets.Contains(this.control))
            {
                return false;
            }

            return true;
        }

        private void CheckAttack()
        {
            foreach (AttackInfo info in AttackManager.Instance.CurrentAttacks)
            {
                if (AttackIsValid(info))
                {
                    if (info.MustCollide)
                    {
                        if (control.animationProgress.CollidingBodyParts.Count != 0)
                        {
                            if (IsCollided(info))
                            {
                                TakeDamage(info);
                            }
                        }
                    }
                    else
                    {
                        if (IsInLethalRange(info))
                        {
                            TakeDamage(info);
                        }
                    }
                }
            }
        }

        private bool IsCollided(AttackInfo info)
        {
            foreach(KeyValuePair<TriggerDetector, List<Collider>> data in
                control.animationProgress.CollidingBodyParts)
            {
                foreach(Collider collider in data.Value)
                {
                    foreach (AttackPartType part in info.AttackParts)
                    {
                        if (info.Attacker.GetAttackingPart(part) ==
                            collider.gameObject)
                        {
                            control.animationProgress.Attack = info.AttackAbility;
                            control.animationProgress.Attacker = info.Attacker;

                            control.animationProgress.DamagedTrigger = data.Key;
                            control.animationProgress.AttackingPart =
                                info.Attacker.GetAttackingPart(part);

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool IsInLethalRange(AttackInfo info)
        {
            foreach(Collider c in control.RagdollParts)
            {
                float dist = Vector3.SqrMagnitude(c.transform.position - info.Attacker.transform.position);

                if (dist <= info.LethalRange)
                {
                    control.animationProgress.Attack = info.AttackAbility;
                    control.animationProgress.Attacker = info.Attacker;

                    int index = Random.Range(0, control.RagdollParts.Count);
                    control.animationProgress.DamagedTrigger = control.RagdollParts[index].GetComponent<TriggerDetector>();

                    return true;
                }
            }

            return false;
        }

        public bool IsDead()
        {
            if (hp <= 0f)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void TakeDamage(AttackInfo info)
        {
            if (IsDead())
            {
                if (!info.RegisteredTargets.Contains(this.control))
                {
                    info.RegisteredTargets.Add(this.control);
                    control.AddForceToDamagedPart(true);
                }

                return;
            }

            if (info.MustCollide)
            {
                CameraManager.Instance.ShakeCamera(0.3f);

                if (info.AttackAbility.UseDeathParticles)
                {
                    if (info.AttackAbility.ParticleType.ToString().Contains("VFX"))
                    {
                        GameObject vfx =
                            PoolManager.Instance.GetObject(info.AttackAbility.ParticleType);

                        vfx.transform.position =
                            control.animationProgress.AttackingPart.transform.position;

                        vfx.SetActive(true);

                        if (info.Attacker.IsFacingForward())
                        {
                            vfx.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                        }
                        else
                        {
                            vfx.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                        }
                    }
                }
            }

            Debug.Log(info.Attacker.gameObject.name + " hits: " + this.gameObject.name);

            info.CurrentHits++;
            hp -= info.AttackAbility.Damage;

            AttackManager.Instance.ForceDeregister(control);

            if (IsDead())
            {
                control.animationProgress.RagdollTriggered = true;
                control.GetComponent<BoxCollider>().enabled = false;
                control.ledgeChecker.GetComponent<BoxCollider>().enabled = false;
                control.RIGID_BODY.useGravity = false;

                if (control.aiController != null)
                {
                    control.aiController.gameObject.SetActive(false);
                    control.navMeshObstacle.enabled = false;
                }
            }
            else
            {
                int rand = Random.Range(0, HitReactionList.Count);

                control.SkinnedMeshAnimator.runtimeAnimatorController = null;
                control.SkinnedMeshAnimator.runtimeAnimatorController = HitReactionList[rand];
            }

            if (!info.RegisteredTargets.Contains(this.control))
            {
                info.RegisteredTargets.Add(this.control);
            }
        }

        public void TriggerSpikeDeath(RuntimeAnimatorController animator)
        {
            control.SkinnedMeshAnimator.runtimeAnimatorController = animator;
        }

        public void DeathBySpikes()
        {
            control.animationProgress.DamagedTrigger = null;
            hp = 0f;
        }
    }
}