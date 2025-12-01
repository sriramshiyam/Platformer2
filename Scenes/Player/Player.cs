using System.Collections.Generic;
using Godot;

namespace Platformer2;

public partial class Player : CharacterBody2D
{
	const float JUMP_SPEED = -280f;
	const float RUN_SPEED = 200f;
	const string ATTACK1_PARAMETER_PATH = "parameters/Ground/conditions/attack1";
	const string ATTACK2_PARAMETER_PATH = "parameters/Ground/Attack/conditions/attack2";
	const string ATTACK3_PARAMETER_PATH = "parameters/Ground/Attack/conditions/attack3";
	const string IDLE_PARAMETER_PATH = "parameters/Ground/conditions/idle";
	float Gravity;
	Sprite2D sprite;
	AnimationTree animationTree;
	AnimationPlayer animationPlayer;
	Timer attackTimer;
	Camera camera;
	float attack1AnimDuration;
	float attack2AnimDuration;
	float attack3AnimDuration;
	bool isAttacking = false;
	List<(string paramPath, float animDuration, bool added)> attackInfoList;
	Dictionary<string, float> attackDeltaForce;

	public override void _Ready()
	{
		attackInfoList = new List<(string, float, bool)>();
		attackDeltaForce = new Dictionary<string, float>();
		Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle() / 1.75f;

		sprite = GetNode<Sprite2D>("Sprite2D");
		camera = GetNode<Camera>("Camera");
		animationTree = GetNode<AnimationTree>("AnimationTree");
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		attackTimer = GetNode<Timer>("AttackTimer");

		attackTimer.Timeout += OnAttackTimerTimeOut;

		StoreAnimationDurationTime();
		StoreAttackDeltaForce();
	}

	public override void _Process(double delta)
	{
		HandleFlip();
		HandleAttack();
	}

	public override void _PhysicsProcess(double delta)
	{
		HandleGravity(delta);
		HandleJump();
		HandleMovement();
		MoveAndSlide();
	}

	private void OnAttackTimerTimeOut()
	{
		if (attackInfoList.Count > 0)
		{
			for (int i = 0; i < attackInfoList.Count; i++)
			{
				var (paramPath, animDuration, added) = attackInfoList[i];
				if (!added)
				{
					animationTree.Set(IDLE_PARAMETER_PATH, false);
					animationTree.Set(paramPath, true);
					attackTimer.Start(animDuration);
					attackInfoList[i] = (paramPath, animDuration, !added);
					CreateAttackForceTween(paramPath);
					return;
				}
			}
		}

		attackInfoList.Clear();

		isAttacking = false;
		animationTree.Set(ATTACK1_PARAMETER_PATH, false);
		animationTree.Set(ATTACK2_PARAMETER_PATH, false);
		animationTree.Set(ATTACK3_PARAMETER_PATH, false);
		animationTree.Set(IDLE_PARAMETER_PATH, true);
	}

	private void StoreAnimationDurationTime()
	{
		attack1AnimDuration = animationPlayer.GetAnimation("attack1").Length;
		attack2AnimDuration = animationPlayer.GetAnimation("attack2").Length;
		attack3AnimDuration = animationPlayer.GetAnimation("attack3").Length;
	}

	private void StoreAttackDeltaForce()
	{
		attackDeltaForce[ATTACK1_PARAMETER_PATH] = 80f;
		attackDeltaForce[ATTACK2_PARAMETER_PATH] = 80f;
		attackDeltaForce[ATTACK3_PARAMETER_PATH] = 200f;
	}

	private void HandleGravity(double delta)
	{
		if (!IsOnFloor())
		{
			Vector2 velocity = Velocity;
			velocity.Y += Gravity * (float)delta;
			Velocity = velocity;
		}
	}

	private void HandleJump()
	{
		if (!isAttacking && Input.IsActionJustPressed("space") && IsOnFloor())
		{
			isAttacking = false;
			Vector2 velocity = Velocity;
			velocity.Y = JUMP_SPEED;
			Velocity = velocity;
		}
	}

	private void HandleMovement()
	{
		if (!isAttacking)
		{
			Vector2 velocity = Velocity;
			float axis = Input.GetAxis("left", "right");
			axis = axis < 0f ? Mathf.Floor(axis) : Mathf.Ceil(axis);
			velocity.X = axis * RUN_SPEED;
			Velocity = velocity;
		}
	}

	private void HandleFlip()
	{
		if (!Mathf.IsZeroApprox(Velocity.X))
		{
			sprite.FlipH = Velocity.X < 0;
			camera.ChangePosition(sprite.FlipH);
		}
	}

	private async void HandleAttack()
	{
		if (!Mathf.IsZeroApprox(Velocity.Y))
		{
			return;
		}

		bool attackButtonPressed = Input.IsActionJustPressed("attack");
		bool canAttack1 = attackButtonPressed && !animationTree.Get(ATTACK1_PARAMETER_PATH).AsBool();

		if (canAttack1)
		{
			isAttacking = true;
			animationTree.Set(IDLE_PARAMETER_PATH, false);
			animationTree.Set(ATTACK1_PARAMETER_PATH, true);
			attackTimer.Start(attack1AnimDuration);
			CreateAttackForceTween(ATTACK1_PARAMETER_PATH);
		}
		else if (attackButtonPressed && attackInfoList.Count < 2)
		{
			if (attackInfoList.Count == 0)
			{
				attackInfoList.Add((ATTACK2_PARAMETER_PATH, attack2AnimDuration, false));
			}
			else
			{
				attackInfoList.Add((ATTACK3_PARAMETER_PATH, attack3AnimDuration, false));
			}
		}
	}

	private void CreateAttackForceTween(string attackPath)
	{
		var tween = CreateTween();

		float deltaForce = attackDeltaForce[attackPath];
		if (sprite.FlipH)
		{
			deltaForce *= -1;
		}

		tween.TweenProperty(this, "velocity", new Vector2(deltaForce, Velocity.Y), 0f);

		float attackDuration;
		if (attackPath == ATTACK1_PARAMETER_PATH)
		{
			attackDuration = attack1AnimDuration;
		}
		else if (attackPath == ATTACK2_PARAMETER_PATH)
		{
			attackDuration = attack2AnimDuration;
		}
		else
		{
			attackDuration = attack3AnimDuration;
		}

		tween.TweenProperty(this, "velocity", new Vector2(0f, Velocity.Y), attackDuration);
	}
}
