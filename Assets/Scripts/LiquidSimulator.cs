using UnityEngine;
using System.Collections.Generic;

public class Liquid {

	// Max and min cell liquid values
	float MaxValue = 1.0f;
	float MinValue = 0.001f;

	// Extra liquid a cell can store than the cell above it
	float MaxCompression = 0.25f;

	// Lowest and highest amount of liquids allowed to flow per iteration
	float MinFlow = 0.05f;
	float MaxFlow = 1f;

	// Adjusts flow speed (0.0f - 1.0f)
	float FlowSpeed = 1f;

	// Keep track of modifications to cell liquid values
	float[,] Diffs;

	public void Initialize(Cell[,] cells) {
		Diffs = new float[cells.GetLength (0), cells.GetLength (1)];
	}

	// Calculate how much liquid should flow to destination with pressure
	float CalculateVerticalFlowValue(float remainingLiquid, Cell destination)
	{
		float sum = remainingLiquid + destination.Liquid;
		float value = 0;

		if (sum <= MaxValue) {
			value = MaxValue;
		} else if (sum < 2 * MaxValue + MaxCompression) {
			value = (MaxValue * MaxValue + sum * MaxCompression) / (MaxValue + MaxCompression);
		} else {
			value = (sum + MaxCompression) / 2f;
		}

		return value;
	}

	// Check if cell contains min required liquid value.  If not clear it.
	float LiquidRemaining(Cell cell, float remainingValue)
	{
		// Cell has less than min value allowed
		if (remainingValue < MinValue) {
			remainingValue = 0;
			cell.Settled = false;
			return remainingValue;
		} else
			return remainingValue;
	}

	// Run one simulation step
	public void Simulate(ref Cell[,] cells) {

		float flow = 0;

		// Reset the diffs array
		for (int x = 0; x < cells.GetLength (0); x++) {
			for (int y = 0; y < cells.GetLength (1); y++) {
				Diffs [x, y] = 0;
			}
		}

		// Main loop
		for (int x = 0; x < cells.GetLength(0); x++) {
			for (int y = 0; y < cells.GetLength(1); y++) {

				// Get reference to Cell and reset flow
				Cell cell = cells [x, y];
				cell.ResetFlowDirections ();

				// Validate cell
				if (cell.Type == CellType.Solid) {
					cell.Liquid = 0;
					continue;
				}
				if (cell.Liquid == 0)
					continue;
				if (cell.Settled) 
					continue;
				if (LiquidRemaining (cell, cell.Liquid) == 0)
					continue;

				// Keep track of how much liquid this cell started off with
				float startValue = cell.Liquid;
				float remainingValue = cell.Liquid;
				flow = 0;

				// Flow to bottom cell
				if (cell.Bottom != null && cell.Bottom.Type == CellType.Blank) {

					// Determine rate of flow
					flow = CalculateVerticalFlowValue(cell.Liquid, cell.Bottom) - cell.Bottom.Liquid;
					if (cell.Bottom.Liquid > 0 && flow > MinFlow)
						flow *= FlowSpeed; 

					// Constrain flow
					flow = Mathf.Max (flow, 0);
					if (flow > Mathf.Min(MaxFlow, cell.Liquid)) 
						flow = Mathf.Min(MaxFlow, cell.Liquid);

					// Update temp values
					if (flow != 0) {
						remainingValue -= flow;
						Diffs [x, y] -= flow;
						Diffs [x, y + 1] += flow;
						cell.FlowDirections[(int)FlowDirection.Bottom] = true;
						cell.Bottom.Settled = false;
					} 
				}

				// Check to ensure we still have liquid in this cell
				if (LiquidRemaining (cell, remainingValue) == 0) {
					continue;
				}

				// Flow to left cell
				if (cell.Left != null && cell.Left.Type == CellType.Blank) {

					// Calculate flow rate
					flow = (remainingValue - cell.Left.Liquid) / 4f;
					if (flow > MinFlow)
						flow *= FlowSpeed;

					// constrain flow
					flow = Mathf.Max (flow, 0);
					if (flow > Mathf.Min(MaxFlow, remainingValue)) 
						flow = Mathf.Min(MaxFlow, remainingValue);

					// Adjust temp values
					if (flow != 0) {
						remainingValue -= flow;
						Diffs [x, y] -= flow;
						Diffs [x - 1, y] += flow;
						cell.FlowDirections[(int)FlowDirection.Left] = true;
						cell.Left.Settled = false;
					} 
				}

				// Check to ensure we still have liquid in this cell
				if (LiquidRemaining (cell, remainingValue) == 0) {
					continue;
				}
				
				// Flow to right cell
				if (cell.Right != null && cell.Right.Type == CellType.Blank) {

					// calc flow rate
					flow = (remainingValue - cell.Right.Liquid) / 3f;										
					if (flow > MinFlow)
						flow *= FlowSpeed; 

					// constrain flow
					flow = Mathf.Max (flow, 0);
					if (flow > Mathf.Min(MaxFlow, remainingValue)) 
						flow = Mathf.Min(MaxFlow, remainingValue);
					
					// Adjust temp values
					if (flow != 0) {
						remainingValue -= flow;
						Diffs [x, y] -= flow;
						Diffs [x + 1, y] += flow;
						cell.FlowDirections[(int)FlowDirection.Right] = true;
						cell.Right.Settled = false;
					} 
				}

				// Check to ensure we still have liquid in this cell
				if (LiquidRemaining (cell, remainingValue) == 0) {
					continue;
				}
				
				// Flow to Top cell
				if (cell.Top != null && cell.Top.Type == CellType.Blank) {

					flow = remainingValue - CalculateVerticalFlowValue (remainingValue, cell.Top); 
					if (flow > MinFlow)
						flow *= FlowSpeed; 

					// constrain flow
					flow = Mathf.Max (flow, 0);
					if (flow > Mathf.Min(MaxFlow, remainingValue)) 
						flow = Mathf.Min(MaxFlow, remainingValue);

					// Adjust values
					if (flow != 0) {
						remainingValue -= flow;
						Diffs [x, y] -= flow;
						Diffs [x, y - 1] += flow;
						cell.FlowDirections[(int)FlowDirection.Top] = true;
						cell.Top.Settled = false;
					} 
				}

				// Check to ensure we still have liquid in this cell
				if (LiquidRemaining (cell, remainingValue) == 0) {
					continue;
				}

				// Check if cell is settled
				if (startValue == remainingValue) {
					cell.SettleCount++;
					if (cell.SettleCount >= 10) {
						cell.ResetFlowDirections ();
						cell.Settled = true;
					}
				} else {
					cell.UnsettleNeighbors ();
				}
			}
		}
			
		// Update Cell values
		for (int x = 0; x < cells.GetLength (0); x++) {
			for (int y = 0; y < cells.GetLength (1); y++) {
				if (Diffs[x,y] != 0)
					cells [x, y].Liquid += Diffs [x, y];
			}
		}			
	}

}
