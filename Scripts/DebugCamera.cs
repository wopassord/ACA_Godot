using Godot;

public partial class DebugCamera : Camera3D
{
	[Export] public float speed = 2.0f;
	[Export] public float sensitivity = 0.005f;

	public override void _Ready()
	{
		// Captura el ratón para poder mirar alrededor
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Control de la mirada con el ratón
		if (@event is InputEventMouseMotion mouseMotion)
		{
			Vector3 rot = Rotation;
			rot.Y -= mouseMotion.Relative.X * sensitivity;
			rot.X -= mouseMotion.Relative.Y * sensitivity;
			// Limitamos mirar muy arriba o muy abajo para no dar vueltas
			rot.X = Mathf.Clamp(rot.X, -Mathf.Pi / 2, Mathf.Pi / 2);
			Rotation = rot;
		}
		
		// Presiona ESCAPE para liberar el ratón y poder cerrar el juego
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
	}

	public override void _Process(double delta)
	{
		// Control de movimiento libre en el espacio 3D (Teclas W A S D + Q E)
		Vector3 velocity = Vector3.Zero;
		
		if (Input.IsPhysicalKeyPressed(Key.W)) velocity += -Transform.Basis.Z;
		if (Input.IsPhysicalKeyPressed(Key.S)) velocity += Transform.Basis.Z;
		if (Input.IsPhysicalKeyPressed(Key.A)) velocity += -Transform.Basis.X;
		if (Input.IsPhysicalKeyPressed(Key.D)) velocity += Transform.Basis.X;
		if (Input.IsPhysicalKeyPressed(Key.E)) velocity += Transform.Basis.Y; // Subir
		if (Input.IsPhysicalKeyPressed(Key.Q)) velocity += -Transform.Basis.Y; // Bajar

		Position += velocity.Normalized() * speed * (float)delta;
	}
}
