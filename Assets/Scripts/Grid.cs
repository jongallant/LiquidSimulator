using UnityEngine;
using System.Collections.Generic;

public class Grid : MonoBehaviour {

	int Width = 80;
	int Height = 40;

	[SerializeField]
	[Range(0.1f, 1f)]
	float CellSize = 1;
	float PreviousCellSize = 1;

	[SerializeField]
	[Range(0f, 0.1f)]
	float LineWidth = 0;
	float PreviousLineWidth = 0;

	[SerializeField]
	Color LineColor = Color.black;
	Color PreviousLineColor = Color.black;

	[SerializeField]
	bool ShowFlow = true;

	[SerializeField]
	bool RenderDownFlowingLiquid = false;

	[SerializeField]
	bool RenderFloatingLiquid = false;

	Cell[,] Cells;
	GridLine[] HorizontalLines;
	GridLine[] VerticalLines;

	Liquid LiquidSimulator;
	Sprite[] LiquidFlowSprites;

	GameObject View;

	bool Fill;

	void Awake() {

		// Camera view
		View = GameObject.Find ("View").gameObject;

		// Load some sprites to show the liquid flow directions
		LiquidFlowSprites = Resources.LoadAll <Sprite>("LiquidFlowSprites");

		// Generate our viewable grid GameObjects
		CreateGrid ();

		// Initialize the liquid simulator
		LiquidSimulator = new Liquid ();
		LiquidSimulator.Initialize (Cells);
	}

	void CreateGrid() {

		Cells = new Cell[Width, Height];
		Vector2 offset = this.transform.position;

		// Organize the grid objects
		GameObject gridLineContainer = new GameObject ("GridLines");
		GameObject cellContainer = new GameObject ("Cells");
		gridLineContainer.transform.parent = this.transform;
		cellContainer.transform.parent = this.transform;

		// vertical grid lines
		VerticalLines = new GridLine[Width + 1];
		for (int x = 0; x < Width + 1; x++) {
			GridLine line = (GameObject.Instantiate (Resources.Load ("GridLine") as GameObject)).GetComponent<GridLine> ();
			float xpos = offset.x + (CellSize * x) + (LineWidth * x);
			line.Set (LineColor, new Vector2 (xpos, offset.y), new Vector2 (LineWidth, (Height*CellSize) + LineWidth * Height + LineWidth));
			line.transform.parent = gridLineContainer.transform;
			VerticalLines [x] = line;
		}

		// horizontal grid lines
		HorizontalLines = new GridLine[Height + 1];
		for (int y = 0; y < Height + 1; y++) {
			GridLine line = (GameObject.Instantiate (Resources.Load ("GridLine") as GameObject)).GetComponent<GridLine> ();
			float ypos = offset.y - (CellSize * y) - (LineWidth * y);
			line.Set (LineColor, new Vector2 (offset.x, ypos), new Vector2 ((Width*CellSize) + LineWidth * Width + LineWidth, LineWidth));
			line.transform.parent = gridLineContainer.transform;
			HorizontalLines [y] = line;
		}

		// Cells
		for (int x = 0; x < Width; x++) {
			for (int y = 0; y < Height; y++) {
				Cell cell = (GameObject.Instantiate (Resources.Load ("Cell") as GameObject)).GetComponent<Cell>();
				float xpos = offset.x + (x * CellSize) + (LineWidth * x) + LineWidth;
				float ypos = offset.y - (y * CellSize) - (LineWidth * y) - LineWidth;
				cell.Set (x, y, new Vector2 (xpos, ypos), CellSize, LiquidFlowSprites, ShowFlow, RenderDownFlowingLiquid, RenderFloatingLiquid);

				// add a border
				if (x == 0 || y == 0 || x == Width - 1 || y == Height - 1) {
					cell.SetType ( CellType.Solid );
				}

				cell.transform.parent = cellContainer.transform;
				Cells [x, y] = cell;
			}
		}
		UpdateNeighbors ();
	}

	// Live update the grid properties
	void RefreshGrid() {
		
		Vector2 offset = this.transform.position;

		// vertical grid lines
		for (int x = 0; x < Width + 1; x++) {
			float xpos = offset.x + (CellSize * x) + (LineWidth * x);
			VerticalLines [x].Set (LineColor, new Vector2 (xpos, offset.y), new Vector2 (LineWidth, (Height*CellSize) + LineWidth * Height + LineWidth));
		}

		// horizontal grid lines
		for (int y = 0; y < Height + 1; y++) {
			float ypos = offset.y - (CellSize * y) - (LineWidth * y);
			HorizontalLines [y] .Set (LineColor, new Vector2 (offset.x, ypos), new Vector2 ((Width*CellSize) + LineWidth * Width + LineWidth, LineWidth));
		}

		// Cells
		for (int x = 0; x < Width; x++) {
			for (int y = 0; y < Height; y++) {
				float xpos = offset.x + (x * CellSize) + (LineWidth * x) + LineWidth;
				float ypos = offset.y - (y * CellSize) - (LineWidth * y) - LineWidth;
				Cells [x, y].Set (x, y, new Vector2 (xpos, ypos), CellSize, LiquidFlowSprites, ShowFlow, RenderDownFlowingLiquid, RenderFloatingLiquid);

			}
		}

		// Fit camera to grid
		View.transform.position = this.transform.position + new Vector3(HorizontalLines [0].transform.localScale.x/2f, -VerticalLines [0].transform.localScale.y/2f);
		View.transform.localScale = new Vector2 (HorizontalLines [0].transform.localScale.x, VerticalLines [0].transform.localScale.y);
		Camera.main.GetComponent<Camera2D> ().Set ();
	}

	// Sets neighboring cell references
	void UpdateNeighbors() {
		for (int x = 0; x < Width; x++) {
			for (int y = 0; y < Height; y++) {
				if (x > 0) {
					Cells[x, y].Left = Cells [x - 1, y];
				}
				if (x < Width - 1) {
					Cells[x, y].Right = Cells [x + 1, y];
				}
				if (y > 0) {
					Cells[x, y].Top = Cells [x, y - 1];
				}
				if (y < Height - 1) {
					Cells[x, y].Bottom = Cells [x, y + 1];
				}
			}
		}
	}

	void Update () {

		// Update grid lines and cell size
		if (PreviousCellSize != CellSize || PreviousLineColor != LineColor || PreviousLineWidth != LineWidth) {
			RefreshGrid ();
		}

		// Convert mouse position to Grid Coordinates
		Vector2 pos = Camera.main.ScreenToWorldPoint (Input.mousePosition);
		int x = (int)((pos.x - this.transform.position.x) / (CellSize + LineWidth));
		int y = -(int)((pos.y - this.transform.position.y) / (CellSize + LineWidth));

		// Check if we are filling or erasing walls
		if (Input.GetMouseButtonDown (0)) {
			if ((x > 0 && x < Cells.GetLength (0)) && (y > 0 && y < Cells.GetLength (1))) {
				if (Cells [x, y].Type == CellType.Blank) {
					Fill = true;
				} else {
					Fill = false;
				}
			}
		}

		// Left click draws/erases walls
		if (Input.GetMouseButton (0)) {		
			if (x != 0 && y != 0 && x != Width - 1 && y != Height - 1) {	
				if ((x > 0 && x < Cells.GetLength (0)) && (y > 0 && y < Cells.GetLength (1))) {
					if (Fill) {						
						Cells [x, y].SetType(CellType.Solid);
					} else {
						Cells [x, y].SetType(CellType.Blank);
					}
				}
			}
		}

		// Right click places liquid
		if (Input.GetMouseButton(1)) {
			if ((x > 0 && x < Cells.GetLength (0)) && (y > 0 && y < Cells.GetLength (1))) {
				Cells [x, y].AddLiquid (5);
			}
		}

		// Run our liquid simulation 
		LiquidSimulator.Simulate (ref Cells);
	}

}
