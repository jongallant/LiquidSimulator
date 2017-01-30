using UnityEngine;

public enum CellType {
	Blank,
	Solid
}

public enum FlowDirection { 
	Top = 0, 
	Right = 1,
	Bottom = 2, 
	Left = 3
}

public class Cell : MonoBehaviour {

	// Grid index reference
	public int X { get ; private set; }
	public int Y { get; private set; }

	// Amount of liquid in this cell
	public float Liquid { get; set; }

	// Determines if Cell liquid is settled
	private bool _settled;
	public bool Settled { 
		get { return _settled; } 
		set {
			_settled = value; 
			if (!_settled) {
				SettleCount = 0;
			}
		}
	}
	public int SettleCount { get; set; }

	public CellType Type { get; private set; }

	// Neighboring cells
	public Cell Top;
	public Cell Bottom { get; set; }
	public Cell Left { get; set; }
	public Cell Right { get; set; }

	// Shows flow direction of cell
	public int Bitmask { get; set; }
	public bool[] FlowDirections = new bool[4];

	// Liquid colors
	Color Color;
	Color DarkColor = new Color (0, 0.1f, 0.2f, 1);

	SpriteRenderer BackgroundSprite;
	SpriteRenderer LiquidSprite;
	SpriteRenderer FlowSprite;

	Sprite[] FlowSprites;

	bool ShowFlow;
	bool RenderDownFlowingLiquid;
	bool RenderFloatingLiquid;

	void Awake() {
		BackgroundSprite = transform.Find ("Background").GetComponent<SpriteRenderer> ();
		LiquidSprite = transform.Find ("Liquid").GetComponent<SpriteRenderer> ();
		FlowSprite = transform.Find ("Flow").GetComponent<SpriteRenderer> ();
		Color = LiquidSprite.color;
	}

	public void Set(int x, int y, Vector2 position, float size, Sprite[] flowSprites, bool showflow, bool renderDownFlowingLiquid, bool renderFloatingLiquid) {
		
		X = x;
		Y = y;

		RenderDownFlowingLiquid = renderDownFlowingLiquid;
		RenderFloatingLiquid = renderFloatingLiquid;
		ShowFlow = showflow;
		FlowSprites = flowSprites;
		transform.position = position;
		transform.localScale = new Vector2 (size, size);

		FlowSprite.sprite = FlowSprites [0];
	}
		
	public void SetType(CellType type) {
		Type = type;
		if (Type == CellType.Solid) {
			Liquid = 0;
		}
		UnsettleNeighbors ();
	}

	public void AddLiquid(float amount) {
		Liquid += amount;
		Settled = false;
	}

	public void ResetFlowDirections() {
		FlowDirections [0] = false;
		FlowDirections [1] = false;
		FlowDirections [2] = false;
		FlowDirections [3] = false;
	}

	// Force neighbors to simulate on next iteration
	public void UnsettleNeighbors() {
		if (Top != null)
			Top.Settled = false;
		if (Bottom != null)
			Bottom.Settled = false;
		if (Left != null)
			Left.Settled = false;
		if (Right != null)
			Right.Settled = false;
	}

	public void Update() {

		// Set background color based on cell type
		if (Type == CellType.Solid) {
			BackgroundSprite.color = Color.black;
		} else {
			BackgroundSprite.color = Color.white;
		}

		// Update bitmask based on flow directions
		Bitmask = 0;
		if (FlowDirections [(int)FlowDirection.Top])
			Bitmask += 1;
		if (FlowDirections [(int)FlowDirection.Right])
			Bitmask += 2;
		if (FlowDirections [(int)FlowDirection.Bottom])
			Bitmask += 4;
		if (FlowDirections [(int)FlowDirection.Left])
			Bitmask += 8;
		
		if (ShowFlow) {
			// Show flow direction of this cell
			FlowSprite.sprite = FlowSprites [Bitmask];
		} else {
			FlowSprite.sprite = FlowSprites [0];
		}

		// Set size of Liquid sprite based on liquid value
		LiquidSprite.transform.localScale = new Vector2 (1, Mathf.Min (1, Liquid));	

		// Optional rendering flags
		if (!RenderFloatingLiquid) {
			// Remove "Floating" liquids
			if (Bottom != null && Bottom.Type != CellType.Solid && Bottom.Liquid <= 0.99f) {
				LiquidSprite.transform.localScale = new Vector2 (0, 0);	
			}
		}
		if (RenderDownFlowingLiquid) {
			// Fill out cell if cell above it has liquid
			if (Type == CellType.Blank && Top != null && (Top.Liquid > 0.05f || Top.Bitmask == 4)) {
				LiquidSprite.transform.localScale = new Vector2 (1, 1);	
			}
		}

		// Set color based on pressure in cell
		LiquidSprite.color = Color.Lerp (Color, DarkColor, Liquid / 4f);
	}

}
