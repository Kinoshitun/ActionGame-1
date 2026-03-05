using UnityEngine;
using Unity.AI.Navigation; // もしNavMeshSurfaceを使う場合は後で必要になります

public class SimpleDungeonGenerator : MonoBehaviour
{
    [Header("Prefabs (空の場合は自動で白いCubeを作ります)")]
    [Tooltip("床に使うプレハブ(Cube等)")]
    [SerializeField] private GameObject floorPrefab;
    [Tooltip("壁に使うプレハブ(Cube等)")]
    [SerializeField] private GameObject wallPrefab;

    [Header("Settings")]
    [Tooltip("1マスの大きさ(メートル)")]
    [SerializeField] private float gridSize = 2f; 
    [Tooltip("壁の高さ")]
    [SerializeField] private float wallHeight = 3f;

    [Header("Map Design")]
    [Tooltip("マップの設計図\n# = 壁\n. = 通路（床のみ）\nS = スタート地点\nB = ボス部屋の中央\n(スペースや空行は無視されます)")]
    [TextArea(10, 20)]
    public string mapData = 
@"
#######
#S....#
###.#.#
###.#.#
#...#.#
#.###.#
#.....#
#####.#####
    #.....#
    #.B...#
    #.....#
    #######
";

    // 生成したマップパーツをまとめる親オブジェクト
    private Transform mapContainer;

    void Start()
    {
        GenerateMap();
    }

    public void GenerateMap()
    {
        // 既にマップがあれば消す（再生成用）
        if (mapContainer != null)
        {
            Destroy(mapContainer.gameObject);
        }
        
        mapContainer = new GameObject("GeneratedDungeon").transform;
        mapContainer.SetParent(this.transform);

        // テキストを改行ごとに分割
        string[] lines = mapData.Trim().Split('\n');
        
        for (int z = 0; z < lines.Length; z++)
        {
            string row = lines[z].Replace("\r", "");
            
            for (int x = 0; x < row.Length; x++)
            {
                char tile = row[x];
                
                // UnityのZ軸（奥）と配列の行を合わせるため、Z座標は反転させる
                Vector3 position = new Vector3(x * gridSize, 0, (lines.Length - z) * gridSize);

                switch (tile)
                {
                    case '#': // 壁（床＋壁）
                        CreateFloor(position);
                        CreateWall(position);
                        break;
                    case '.': // 通路（床のみ）
                        CreateFloor(position);
                        break;
                    case 'S': // スタート地点
                        CreateFloor(position);
                        // プレイヤーをここに移動させる処理を後で追加できます
                        Debug.Log($"スタート地点: {position}");
                        break;
                    case 'B': // ボス部屋の中央
                        CreateFloor(position);
                        // ボスキャラをここに生成する処理を後で追加できます
                        Debug.Log($"ボス部屋: {position}");
                        break;
                    case ' ':
                        // 空白は何も置かない（奈落）
                        break;
                }
            }
        }

        Debug.Log("ダンジョンの生成が完了しました！");
    }

    private void CreateFloor(Vector3 pos)
    {
        GameObject floor;
        if (floorPrefab != null)
        {
            floor = Instantiate(floorPrefab, pos, Quaternion.identity, mapContainer);
        }
        else
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = pos;
            floor.transform.SetParent(mapContainer);
        }
        
        // サイズ調整（Y軸は薄くする）
        floor.transform.localScale = new Vector3(gridSize, 0.5f, gridSize);
        // 床の上面がY=0になるように少し下げる
        floor.transform.position += new Vector3(0, -0.25f, 0);
    }

    private void CreateWall(Vector3 pos)
    {
        GameObject wall;
        if (wallPrefab != null)
        {
            wall = Instantiate(wallPrefab, pos, Quaternion.identity, mapContainer);
        }
        else
        {
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = pos;
            wall.transform.SetParent(mapContainer);
            // 仮の壁は少し色を暗くする
            wall.GetComponent<Renderer>().material.color = Color.gray;
        }

        wall.transform.localScale = new Vector3(gridSize, wallHeight, gridSize);
        // 壁が床の上に乗るようにY座標を調整
        wall.transform.position += new Vector3(0, wallHeight / 2f, 0);
    }
}
