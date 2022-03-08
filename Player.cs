using Godot;
using System;
using System.Collections;

public class Player : KinematicBody2D
{
  [Export]
  public float Thrust = 1f; // effective acceleration

  [Export]
  public float MaxSpeed = 5;

  [Export]
  public float StopThreshold = 0.5f;
  
  [Export]
  public float CurrentVelocity = 0;

  [Export]
  public float RotationThrust = 1.5f;

  [Export]
  public float CurrentRotation = 0;

  [Export]
  public int HitPoints = 100;

  public Queue MovementQueue = new Queue();

  public String uuid;

  CSLogger cslogger;

  // for now only one missile at a time
  SpaceMissile MyMissile = null;

  [Export] 
  int MissileSpeed = 300;
  
  [Export]
  float MissileLife = 4;

  [Export]
  int MissileDamage = 25;

  public void ExpireMissile() { MyMissile = null; }

  public void FireMissile()
  {
    // only one missile allowed for now
    if (MyMissile != null) { return; }

    PackedScene missileScene = (PackedScene)ResourceLoader.Load("res://SpaceMissile.tscn");
    MyMissile = (SpaceMissile)missileScene.Instance();

    // missile should point in the same direction as the ship
    MyMissile.Rotation = Rotation;
    
    // TODO: need to offset this to the front of the ship
    // start at our position
    MyMissile.Position = Position;

    // negative direction is "up"
    Vector2 offset = new Vector2(0, -100);

    // rotate the offset to match the current ship heading
    offset = offset.Rotated(Rotation);
    MyMissile.Position = MyMissile.Position + offset;

    // set missile's parameters based on current modifiers
    MyMissile.MissileSpeed = MissileSpeed;
    MyMissile.MissileLife = MissileLife;
    MyMissile.MissileDamage = MissileDamage;

    // this is a poop way to do this
    MyMissile.MyPlayer = this;

    Node rootNode = GetNode<Node>("/root");
    rootNode.AddChild(MyMissile);
    cslogger.Debug("Added missile instance!");
  }

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    // initialize the logging configuration
    Node gdlogger = GetNode<Node>("/root/GDLogger");
    gdlogger.Call("load_config", "res://logger.cfg");
    cslogger = GetNode<CSLogger>("/root/CSLogger");

    Label playerIDLabel = (Label)GetNode("IDLabel");

    // TODO: deal with really long UUIDs
    playerIDLabel.Text = uuid;
  }

  public void TakeDamage(int Damage)
  {
    cslogger.Debug($"Player.cs: {uuid}: Taking damage: {Damage}");
    HitPoints -= Damage;
    cslogger.Debug($"Player.cs: {uuid}: Hitpoints: {HitPoints}");
  }

  void RemovePlayer()
  {
    cslogger.Verbose($"Player.cs: removing {uuid}");
    Server theServer = (Server)GetNode("/root/Server");
    theServer.RemovePlayer(uuid);
  }

  public override void _Process(float delta)
  {
    if (HitPoints <= 0)
    {
      cslogger.Debug("Hitpoints zeroed! Remove the player!");
      RemovePlayer();
    }
  }
  public override void _PhysicsProcess(float delta)
  {
    // somewhat based on: https://kidscancode.org/godot_recipes/2d/topdown_movement/
    // "rotate and move" / asteroids-style-ish

    Label angularVelocityLabel = (Label)GetNode("AngularVelocity");
    Label linearVelocityLabel = (Label)GetNode("LinearVelocity");

    angularVelocityLabel.Text = $"Ang: {CurrentRotation}";
    linearVelocityLabel.Text = $"Lin: {CurrentVelocity}";

    float rotation_dir = 0; // in case we need it

    cslogger.Verbose($"{uuid}: handling physics");
    if (MovementQueue.Count > 0)
    {
      Vector2 thisMovement = (Vector2)MovementQueue.Dequeue();
      cslogger.Verbose($"UUID: {uuid} X: {thisMovement.x} Y: {thisMovement.y}");

      if (thisMovement.y > 0)
      {
        CurrentVelocity = Mathf.Lerp(CurrentVelocity, MaxSpeed, Thrust * delta);
      }

      if (thisMovement.y < 0)
      {
        CurrentVelocity = Mathf.Lerp(CurrentVelocity, 0, Thrust * delta);

        // cut speed when velocity gets below threshold, otherwise LERPing
        // results in never actually stopping. 
        if (CurrentVelocity < StopThreshold) { CurrentVelocity = 0; }
      }

      if (thisMovement.x != 0)
      {
        rotation_dir = thisMovement.x;
      }

      cslogger.Verbose($"UUID: {uuid} Velocity: {CurrentVelocity}");

    }
    Vector2 velocity =  -(Transform.y * CurrentVelocity);
    cslogger.Verbose($"UUID: {uuid} Vector X: {velocity.x} Y: {velocity.y} ");
    Rotation += rotation_dir * RotationThrust * delta;
    MoveAndCollide(velocity);
  }

}