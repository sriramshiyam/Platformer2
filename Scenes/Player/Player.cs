using System.Collections.Generic;
using Godot;

namespace Platformer2;

public partial class Player : CharacterBody2D
{
	#region MOVEMENT
	const float JUMP_SPEED = -300f;
	const float RUN_SPEED = 200f;
	float Gravity;
	#endregion

	#region ANIMATION_TREE_PARAM
	const string ATTACK_PARAMETER_PATH = "parameters/Ground/conditions/attack";
	const string ARROW_ATTACK_PARAMETER_PATH = "parameters/Ground/Attack/conditions/arrow_attack";
	const string ATTACK1_PARAMETER_PATH = "parameters/Ground/Attack/conditions/attack1";
	const string ATTACK2_PARAMETER_PATH = "parameters/Ground/Attack/conditions/attack2";
	const string ATTACK3_PARAMETER_PATH = "parameters/Ground/Attack/conditions/attack3";
	const string IDLE_PARAMETER_PATH = "parameters/Ground/conditions/idle";
	#endregion

	#region ANIMATION;
	Sprite2D sprite;
	AnimationTree animationTree;
	AnimationPlayer animationPlayer;
	#endregion

	Camera camera;

	#region ATTACK
	Timer arrowSpawnTimer;
	float arrowSpawnTime;
	Marker2D arrowSpawner;
	Timer attackTimer;
	float attack1AnimDuration;
	float attack2AnimDuration;
	float attack3AnimDuration;
	float arrowAttackDuration;
	bool isAttacking = false;
	Tween attackForceTween = null;
	#endregion

	List<(string paramPath, float animDuration, bool added)> attackInfoList;
	Dictionary<string, float> attackDeltaForce;

	public override void _Ready()
	{
		attackInfoList = new List<(string, float, bool)>();
		attackDeltaForce = new Dictionary<string, float>();
		Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

		sprite = GetNode<Sprite2D>("Sprite2D");
		camera = GetNode<Camera>("Camera");
		animationTree = GetNode<AnimationTree>("AnimationTree");
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		attackTimer = GetNode<Timer>("AttackTimer");
		arrowSpawnTimer = GetNode<Timer>("ArrowSpawnTimer");
		arrowSpawner = GetNode<Marker2D>("ArrowSpawner");
		arrowSpawnTime = 0.7f;

		attackTimer.Timeout += OnAttackTimerTimeOut;
		arrowSpawnTimer.Timeout += OnArrowSpawnTimerTimeOut;

		StoreAnimationDurationTime();
		StoreAttackDeltaForce();
	}

	private void OnArrowSpawnTimerTimeOut()
	{
		SignalBus.I.EmitSpawnArrow(arrowSpawner.GlobalPosition, sprite.FlipH);
	}

	public override void _Process(double delta)
	{
		HandleFlip();
		HandleAttack();
		HandleArrowAttack();
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

		GD.Print("ATTACK CLEARED");
		ClearAttackParam();
	}

	private void ClearAttackParam()
	{
		attackInfoList.Clear();

		isAttacking = false;
		animationTree.Set(ATTACK_PARAMETER_PATH, false);
		animationTree.Set(ARROW_ATTACK_PARAMETER_PATH, false);
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
		arrowAttackDuration = animationPlayer.GetAnimation("arrow_attack").Length;
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
			if (attackForceTween != null && attackForceTween.IsRunning())
			{
				GD.Print("TWEEN STOPPED");
				attackForceTween.Stop();
				ClearAttackParam();
			}

			float gravity = Velocity.Y < 0f ? (Gravity / 1.7f) : (Gravity / 2f);
			Vector2 velocity = Velocity;
			velocity.Y += gravity * (float)delta;
			Velocity = velocity;
		}
	}

	private void HandleJump()
	{
		if (!isAttacking && Input.IsActionJustPressed("jump") && IsOnFloor())
		{
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

	private void HandleAttack()
	{
		if (!IsOnFloor() || animationTree.Get(ARROW_ATTACK_PARAMETER_PATH).AsBool())
		{
			return;
		}

		bool attackButtonPressed = Input.IsActionJustPressed("attack");
		bool canAttack1 = attackButtonPressed && !animationTree.Get(ATTACK1_PARAMETER_PATH).AsBool();

		if (canAttack1)
		{
			isAttacking = true;
			animationTree.Set(IDLE_PARAMETER_PATH, false);
			animationTree.Set(ATTACK_PARAMETER_PATH, true);
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

	private void HandleArrowAttack()
	{
		if (!IsOnFloor() || animationTree.Get(ATTACK1_PARAMETER_PATH).AsBool())
		{
			return;
		}

		bool arrowAttackButtonPressed = Input.IsActionJustPressed("arrow_attack");
		bool canArrowAttack = arrowAttackButtonPressed && !animationTree.Get(ARROW_ATTACK_PARAMETER_PATH).AsBool();

		if (canArrowAttack)
		{
			Velocity = Vector2.Zero;
			isAttacking = true;
			animationTree.Set(IDLE_PARAMETER_PATH, false);
			animationTree.Set(ATTACK_PARAMETER_PATH, true);
			animationTree.Set(ARROW_ATTACK_PARAMETER_PATH, true);
			attackTimer.Start(arrowAttackDuration);
			arrowSpawnTimer.Start(arrowSpawnTime);
		}
	}

	private void CreateAttackForceTween(string attackPath)
	{
		attackForceTween = CreateTween();

		float deltaForce = attackDeltaForce[attackPath];
		if (sprite.FlipH)
		{
			deltaForce *= -1;
		}

		attackForceTween.TweenProperty(this, "velocity", new Vector2(deltaForce, Velocity.Y), 0f);

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

		attackForceTween.TweenProperty(this, "velocity", new Vector2(0f, Velocity.Y), attackDuration);
	}
}
