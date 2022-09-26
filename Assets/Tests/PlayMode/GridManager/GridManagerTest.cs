using System.Collections;
using Altzone.Scripts.Config;
using Battle.Scripts.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

namespace Tests.PlayMode.GridManager
{
    public class GridManagerTest
    {
        [OneTimeSetUp]
        public void LoadScene()
        {
            SceneManager.LoadScene("ut-GridManagerTest");
        }

        [UnityTest]
        public IEnumerator GridManagerTestWithEnumeratorPasses()
        {
            // Use yield to skip a frame.
            var runtimeGameConfig = RuntimeGameConfig.Get();
            Assert.AreEqual(false, runtimeGameConfig.Features._isDisableBattleGridMovement);

            IGridManager gridManager;
            for (;;)
            {
                // Wait until grid manager is initialized.
                gridManager = Context.GetGridManager;
                if (gridManager.RowCount > 0)
                {
                    break;
                }
                yield return null;
            }
            
            var variables = runtimeGameConfig.Variables;
            var gridWidth = variables._battleUiGridWidth;
            var gridHeight = variables._battleUiGridHeight;
            
            var battleCamera = Context.GetBattleCamera;
            var world = battleCamera.Camera.ViewportToWorldPoint(Vector3.one);
            var worldWidth = 2f * world.x;
            var worldHeight = 2f * world.y;
            Debug.Log($"GRID rows {gridWidth} cols {gridHeight} WORLD width {worldWidth} height {worldHeight}");

            // We require that play are is set on origo (0,0) - to check that world pos is inside our "grid" world
            var battlePlayArea = Context.GetBattlePlayArea;
            Assert.AreEqual(Vector2.zero, battlePlayArea.GetPlayAreaCenterPosition);

            Assert.AreEqual(gridHeight, gridManager.RowCount);
            Assert.AreEqual(gridWidth, gridManager.ColCount);
            yield return null;
            var rowMax = gridHeight;
            var colMax = gridWidth;
            var expectedState = false;
            foreach (var rotation in new[] { false, true })
            {
                Debug.Log($"Grid rotation {rotation} expectedState {expectedState}");
                for (var row = 0; row < rowMax; ++row)
                {
                    for (var col = 0; col < colMax; ++col)
                    {
                        var gridPos = new GridPos(row, col);
                        var worldPos = gridManager.GridPositionToWorldPoint(gridPos, rotation);
                        Debug.Log($"Grid row, col {row:00},{col:00} -> x,y {worldPos.x:0.00},{worldPos.y:0.00} ({worldPos.x},{worldPos.y})");
                        Assert.IsFalse(Mathf.Abs(worldPos.x) > world.x);
                        Assert.IsFalse(Mathf.Abs(worldPos.y) > world.y);
                        var gridPos2 = gridManager.WorldPointToGridPosition(worldPos, rotation);
                        Assert.AreEqual(row, gridPos2.Row);
                        Assert.AreEqual(col, gridPos2.Col);
                        var currentState = gridManager.GridState(row, col);
                        Assert.AreEqual(expectedState, currentState);
                        // Set state so that it will have opposite value on next "round".
                        gridManager.SetGridState(row, col, !currentState);
                    }
                }
                expectedState = !expectedState;
            }
            Debug.Log("Done");
        }
    }
}
