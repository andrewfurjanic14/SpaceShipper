using UnityEngine;

public abstract class PlayerState
{
    protected PlayerController player;

    public PlayerState(PlayerController player) {  this.player = player; }

    public virtual bool CanMove => false;
    public virtual bool CanAttack => false;
    public virtual bool CanDash => false;

    public abstract void EnterState();
    public abstract void UpdateState();
    public virtual void FixedUpdateState() { }
    public virtual void ExitState() { }
}
