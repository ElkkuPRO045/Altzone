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
            IGridManager gridManager;
            for (;;)
            {
                gridManager = Context.GetGridManager;
                if (gridManager?._gridEmptySpaces != null)
                {
                    break;
                }
                yield return null;
            }
            var runtimeGameConfig = RuntimeGameConfig.Get();
            Assert.AreEqual(false, runtimeGameConfig.Features._isDisableBattleGridMovement);
            
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

            var grid = gridManager._gridEmptySpaces;
            Assert.AreEqual(gridWidth + 1, grid.GetLength(1));
            Assert.AreEqual(gridHeight + 1, grid.GetLength(0));
            yield return null;
            var rowMax = gridHeight;
            var colMax = gridWidth;
            foreach (var rotation in new[] { false, true })
            {
                Debug.Log($"Grid rotation {rotation}");
                for (var row = 1; row <= rowMax; ++row)
                {
                    for (var col = 1; col <= colMax; ++col)
                    {
                        var gridPos = new GridPos(row, col);
                        var worldPos = gridManager.GridPositionToWorldPoint(gridPos, rotation);
                        Debug.Log($"Grid row, col {row:00},{col:00} -> x,y {worldPos.x:0.00},{worldPos.y:0.00} ({worldPos.x},{worldPos.y})");
                        Assert.IsFalse(Mathf.Abs(worldPos.x) > world.x);
                        Assert.IsFalse(Mathf.Abs(worldPos.y) > world.y);
                        var gridPos2 = gridManager.WorldPointToGridPosition(worldPos, rotation);
                        Assert.AreEqual(row, gridPos2.Row);
                        Assert.AreEqual(col, gridPos2.Col);
                    }
                }
            }
            Debug.Log("Done");
        }
    }
}
