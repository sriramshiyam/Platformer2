using System.Collections.Generic;
using Godot;

namespace Platformer2;

public partial class Player : CharacterBody2D
{
	#region MOVEMENT
	const float JUMP_SPEED = -350f;
	const float RUN_SPEED = 250f;
	float Gravity;
	#endregion

	#region ANIMATION_TREE_PARAM
	const string ATTACK_PARAM_PATH = "parameters/Ground/conditions/attack";
	const string ARROW_ATTACK_PARAM_PATH = "parameters/Ground/Attack/conditions/arrow_attack";
	const string ATTACK1_PARAM_PATH = "parameters/Ground/Attack/conditions/attack1";
	const string ATTACK2_PARAM_PATH = "parameters/Ground/Attack/conditions/attack2";
	const string ATTACK3_PARAM_PATH = "parameters/Ground/Attack/conditions/attack3";
	const string IDLE_PARAM_PATH = "parameters/Ground/conditions/idle";
	const string AIR_ATTACK_PARAM_PATH = "parameters/Air/conditions/attack";
	const string JUMP_PARAM_PATH = "parameters/Air/conditions/jump";
	const string FALL_PARAM_PATH = "parameters/Air/conditions/fall";
	const string AIR_ATTACK1_PARAM_PATH = "parameters/Air/Attack/conditions/air_attack1";
	const string AIR_ATTACK2_PARAM_PATH = "parameters/Air/Attack/conditions/air_attack2";
	const string AIR_ARROW_ATTACK_PARAM_PATH = "parameters/Air/Attack/conditions/air_arrow_attack";
	#endregion

	#region ANIMATION;
	Sprite2D sprite;
	AnimationTree animationTree;
	AnimationPlayer animationPlayer;
	#endregion

	Camera camera;

	#region AUDIO
	AudioStreamPlayer2D audioStreamPlayer;
	const float ATTACK_DECIBEL = 15f;
	const float ARROW_ATTACK_DECIBEL = 10f;
	const float JUMP_DECIBEL = 8f;
	#endregion

	#region ATTACK
	Timer arrowSpawnTimer;
	const float ARROW_SPAWN_TIME = 0.5f;
	Marker2D arrowSpawner;
	Timer attackTimer;
	float attack1AnimDuration;
	float attack2AnimDuration;
	float attack3AnimDuration;
	float arrowAttackDuration;
	bool isAttacking = false;
	Tween attackForceTween = null;
	#endregion

	#region  AIR_ATTACK
	Timer airAttackTimer;
	float airAttack1AnimDuration;
	float airAttack2AnimDuration;
	float airArrowAttackDuration;
	int canAirAttack2 = 0;
	Marker2D airArrowSpawner;
	const float AIR_ARROW_SPAWN_TIME = 0.3f;
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
		animationTree = GetNode<AnimationTree>("Animation/AnimationTree");
		animationPlayer = GetNode<AnimationPlayer>("Animation/AnimationPlayer");
		attackTimer = GetNode<Timer>("Timers/AttackTimer");
		arrowSpawnTimer = GetNode<Timer>("Timers/ArrowSpawnTimer");
		airAttackTimer = GetNode<Timer>("Timers/AirAttackTimer");
		arrowSpawner = GetNode<Marker2D>("ArrowSpawners/ArrowSpawner");
		airArrowSpawner = GetNode<Marker2D>("ArrowSpawners/AirArrowSpawner");
		audioStreamPlayer = GetNode<AudioStreamPlayer2D>("Sound");

		attackTimer.Timeout += OnAttackTimerTimeOut;
		arrowSpawnTimer.Timeout += OnArrowSpawnTimerTimeOut;
		airAttackTimer.Timeout += OnAirAttackTimerTimeOut;

		StoreAnimationDurationTime();
		StoreAttackDeltaForce();
	}

	public override void _Process(double delta)
	{
		HandleFlip();
		HandleAttack();
		HandleArrowAttack();
		HandleAirAttack();
		HandleAirArrowAttack();
	}

	public override void _PhysicsProcess(double delta)
	{
		HandleGravity(delta);
		HandleJump();
		HandleMovement();
		MoveAndSlide();
	}

	private void OnAirAttackTimerTimeOut()
	{
		if (canAirAttack2 > 0)
		{
			canAirAttack2 = -1;
			animationTree.Advance(0f);
			animationTree.Set(AIR_ATTACK2_PARAM_PATH, true);
			airAttackTimer.Start(airAttack2AnimDuration);
			audioStreamPlayer.VolumeDb = ATTACK_DECIBEL;
			SoundManager.I.PlaySound(audioStreamPlayer, SoundManager.I.ATTACK_SOUND);
			return;
		}

		ClearAirAttackParam();
	}

	private void ClearAirAttackParam()
	{
		canAirAttack2 = 0;
		animationTree.Set(AIR_ATTACK_PARAM_PATH, false);
		animationTree.Set(AIR_ATTACK1_PARAM_PATH, false);
		animationTree.Set(AIR_ATTACK2_PARAM_PATH, false);
		animationTree.Set(AIR_ARROW_ATTACK_PARAM_PATH, false);

		if (Velocity.Y < 0)
		{
			animationTree.Set(JUMP_PARAM_PATH, true);
		}
		else
		{
			animationTree.Set(FALL_PARAM_PATH, true);
		}
	}

	private void OnArrowSpawnTimerTimeOut()
	{
		audioStreamPlayer.VolumeDb = ARROW_ATTACK_DECIBEL;
		SoundManager.I.PlaySound(audioStreamPlayer, SoundManager.I.ARROW_ATTACK_SOUND);
		Vector2 arrowPosition = IsOnFloor() ? arrowSpawner.GlobalPosition : airArrowSpawner.GlobalPosition;
		SignalBus.I.EmitSpawnArrow(arrowPosition, sprite.FlipH);
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
					animationTree.Advance(0f);
					animationTree.Set(paramPath, true);
					attackTimer.Start(animDuration);
					attackInfoList[i] = (paramPath, animDuration, !added);
					CreateAttackForceTween(paramPath);
					audioStreamPlayer.VolumeDb = ATTACK_DECIBEL;
					SoundManager.I.PlaySound(audioStreamPlayer, SoundManager.I.ATTACK_SOUND);
					return;
				}
			}
		}

		ClearAttackParam();
	}

	private void ClearAttackParam()
	{
		attackInfoList.Clear();

		isAttacking = false;
		animationTree.Set(ATTACK_PARAM_PATH, false);
		animationTree.Set(ARROW_ATTACK_PARAM_PATH, false);
		animationTree.Set(ATTACK1_PARAM_PATH, false);
		animationTree.Set(ATTACK2_PARAM_PATH, false);
		animationTree.Set(ATTACK3_PARAM_PATH, false);
		animationTree.Set(IDLE_PARAM_PATH, true);
	}

	private void StoreAnimationDurationTime()
	{
		attack1AnimDuration = animationPlayer.GetAnimation("attack1").Length;
		attack2AnimDuration = animationPlayer.GetAnimation("attack2").Length;
		attack3AnimDuration = animationPlayer.GetAnimation("attack3").Length;
		arrowAttackDuration = animationPlayer.GetAnimation("arrow_attack").Length;
		airAttack1AnimDuration = animationPlayer.GetAnimation("air_attack1").Length;
		airAttack2AnimDuration = animationPlayer.GetAnimation("air_attack2").Length;
		airArrowAttackDuration = animationPlayer.GetAnimation("air_arrow_attack").Length;
	}

	private void StoreAttackDeltaForce()
	{
		attackDeltaForce[ATTACK1_PARAM_PATH] = 80f;
		attackDeltaForce[ATTACK2_PARAM_PATH] = 80f;
		attackDeltaForce[ATTACK3_PARAM_PATH] = 200f;
	}

	private void HandleGravity(double delta)
	{
		if (!IsOnFloor() && !animationTree.Get(AIR_ARROW_ATTACK_PARAM_PATH).AsBool())
		{
			if (attackForceTween != null && attackForceTween.IsRunning())
			{
				attackForceTween.Stop();
				ClearAttackParam();
			}

			// float gravity = Velocity.Y < 0f ? (Gravity / 1.7f) : (Gravity / 2f);
			Vector2 velocity = Velocity;
			velocity.Y += Gravity * (float)delta;
			Velocity = velocity;
		}
	}

	private void HandleJump()
	{
		if (!isAttacking && Input.IsActionJustPressed("jump") && IsOnFloor())
		{
			audioStreamPlayer.VolumeDb = JUMP_DECIBEL;
			SoundManager.I.PlaySound(audioStreamPlayer, SoundManager.I.JUMP_SOUND);
			Vector2 velocity = Velocity;
			velocity.Y = JUMP_SPEED;
			Velocity = velocity;
		}
	}

	private void HandleMovement()
	{
		if (!isAttacking && !animationTree.Get(AIR_ARROW_ATTACK_PARAM_PATH).AsBool())
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
		if (!IsOnFloor() || animationTree.Get(ARROW_ATTACK_PARAM_PATH).AsBool())
		{
			return;
		}

		bool attackButtonPressed = Input.IsActionJustPressed("attack");
		bool canAttack1 = attackButtonPressed && !animationTree.Get(ATTACK1_PARAM_PATH).AsBool();

		if (canAttack1)
		{
			isAttacking = true;
			animationTree.Advance(0f);
			animationTree.Set(IDLE_PARAM_PATH, false);
			animationTree.Set(ATTACK_PARAM_PATH, true);
			animationTree.Set(ATTACK1_PARAM_PATH, true);
			attackTimer.Start(attack1AnimDuration);
			CreateAttackForceTween(ATTACK1_PARAM_PATH);
			audioStreamPlayer.VolumeDb = ATTACK_DECIBEL;
			SoundManager.I.PlaySound(audioStreamPlayer, SoundManager.I.ATTACK_SOUND);
		}
		else if (attackButtonPressed && attackInfoList.Count < 2)
		{
			if (attackInfoList.Count == 0)
			{
				attackInfoList.Add((ATTACK2_PARAM_PATH, attack2AnimDuration, false));
			}
			else
			{
				attackInfoList.Add((ATTACK3_PARAM_PATH, attack3AnimDuration, false));
			}
		}
	}

	private void HandleArrowAttack()
	{
		if (!IsOnFloor() || animationTree.Get(ATTACK1_PARAM_PATH).AsBool())
		{
			return;
		}

		bool arrowAttackButtonPressed = Input.IsActionJustPressed("arrow_attack");
		bool canArrowAttack = arrowAttackButtonPressed && !animationTree.Get(ARROW_ATTACK_PARAM_PATH).AsBool();

		if (canArrowAttack)
		{
			Velocity = Vector2.Zero;
			isAttacking = true;
			animationTree.Advance(0f);
			animationTree.Set(IDLE_PARAM_PATH, false);
			animationTree.Set(ATTACK_PARAM_PATH, true);
			animationTree.Set(ARROW_ATTACK_PARAM_PATH, true);
			attackTimer.Start(arrowAttackDuration);
			arrowSpawnTimer.Start(ARROW_SPAWN_TIME);
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
		if (attackPath == ATTACK1_PARAM_PATH)
		{
			attackDuration = attack1AnimDuration;
		}
		else if (attackPath == ATTACK2_PARAM_PATH)
		{
			attackDuration = attack2AnimDuration;
		}
		else
		{
			attackDuration = attack3AnimDuration;
		}

		attackForceTween.TweenProperty(this, "velocity", new Vector2(0f, Velocity.Y), attackDuration);
	}

	private void HandleAirAttack()
	{
		if (IsOnFloor() || animationTree.Get(AIR_ARROW_ATTACK_PARAM_PATH).AsBool())
		{
			return;
		}

		bool attackButtonPressed = Input.IsActionJustPressed("attack");
		bool canAirAttack = attackButtonPressed && !animationTree.Get(AIR_ATTACK1_PARAM_PATH).AsBool();

		if (canAirAttack)
		{
			animationTree.Advance(0f);
			animationTree.Set(JUMP_PARAM_PATH, false);
			animationTree.Set(FALL_PARAM_PATH, false);
			animationTree.Set(AIR_ATTACK_PARAM_PATH, true);
			animationTree.Set(AIR_ATTACK1_PARAM_PATH, true);
			airAttackTimer.Start(airAttack1AnimDuration);
			audioStreamPlayer.VolumeDb = ATTACK_DECIBEL;
			SoundManager.I.PlaySound(audioStreamPlayer, SoundManager.I.ATTACK_SOUND);
		}
		else if (attackButtonPressed && canAirAttack2 == 0)
		{
			canAirAttack2++;
		}
	}

	private void HandleAirArrowAttack()
	{
		if (IsOnFloor() || animationTree.Get(AIR_ATTACK1_PARAM_PATH).AsBool())
		{
			return;
		}

		bool arrowAttackButtonPressed = Input.IsActionJustPressed("arrow_attack");
		bool canArrowAttack = arrowAttackButtonPressed && !animationTree.Get(AIR_ARROW_ATTACK_PARAM_PATH).AsBool();

		if (canArrowAttack)
		{
			Velocity = Vector2.Zero;
			animationTree.Advance(0f);
			animationTree.Set(JUMP_PARAM_PATH, false);
			animationTree.Set(FALL_PARAM_PATH, false);
			animationTree.Set(AIR_ATTACK_PARAM_PATH, true);
			animationTree.Set(AIR_ARROW_ATTACK_PARAM_PATH, true);
			airAttackTimer.Start(airArrowAttackDuration);
			arrowSpawnTimer.Start(AIR_ARROW_SPAWN_TIME);
		}
	}
}
