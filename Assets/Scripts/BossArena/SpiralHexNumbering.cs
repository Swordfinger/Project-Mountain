using System.Collections.Generic;
using JailerGame.Combat.HexGrid;

namespace JailerGame.BossArena
{
    /// <summary>
    /// 以 Boss 当前格为中心的螺旋编号系统。
    ///
    /// 编号规则（俯视角，正北朝上）：
    ///   0 = Boss 自己所在的格
    ///   第 1 圈（6 格）：1=右上，2=右，3=右下，4=左下，5=左，6=左上
    ///   第 2 圈（12 格）：7~18，从"右上方向再向外一格"开始，沿同一方向继续编号
    ///   第 N 圈：6N 格，从右上外圈起点开始顺时针扫
    ///
    /// Boss 移动后，所有玩家的编号需要重新计算（调用 RebuildFor）。
    /// </summary>
    public class SpiralHexNumbering
    {
        // 与 HexCoordinates.Directions 索引对齐：
        //   0 East / 1 NorthEast / 2 NorthWest / 3 West / 4 SouthWest / 5 SouthEast
        // 你描述的 "右上=1, 右=2, 右下=3, 左下=4, 左=5, 左上=6"，
        // 对应到我们 cube 坐标的方向索引顺序应该是：
        //   1 -> NorthEast(1)
        //   2 -> East(0)
        //   3 -> SouthEast(5)
        //   4 -> SouthWest(4)
        //   5 -> West(3)
        //   6 -> NorthWest(2)
        // 顺时针顺序：NE → E → SE → SW → W → NW
        private static readonly int[] ClockwiseFromNE = { 1, 0, 5, 4, 3, 2 };

        public HexCoordinates Center { get; private set; }
        private readonly Dictionary<HexCoordinates, int> _coordToNumber = new();
        private readonly Dictionary<int, HexCoordinates> _numberToCoord = new();
        public int MaxRingBuilt { get; private set; }

        public SpiralHexNumbering(HexCoordinates bossCenter, int maxRing = 6)
        {
            Rebuild(bossCenter, maxRing);
        }

        /// <summary>Boss 移动后重建编号</summary>
        public void Rebuild(HexCoordinates newCenter, int maxRing = 6)
        {
            Center = newCenter;
            _coordToNumber.Clear();
            _numberToCoord.Clear();
            MaxRingBuilt = maxRing;

            // 0 号 = Boss 自己
            _coordToNumber[newCenter] = 0;
            _numberToCoord[0] = newCenter;

            int counter = 1;
            for (int ring = 1; ring <= maxRing; ring++)
            {
                // 起点：从中心沿"右上(NE)"方向走 ring 格
                var startCoord = newCenter;
                for (int i = 0; i < ring; i++) startCoord = startCoord.Neighbor(1); // NE

                var current = startCoord;

                // 顺时针扫一圈：每条"边"长 ring 格，共 6 条边
                // 边 0：从 NE 起点出发，沿"右(E)"方向走 ring 格 —— 但起点本身就是这条边的第一格，
                //       所以编号顺序为 [起点, 沿 E 走的后续 ring-1 格]，然后转 SE，依此类推。
                // 实际实现：先加入起点，然后沿 6 个顺时针方向各走 ring 格（最后一格回到起点，跳过）。
                _coordToNumber[current] = counter;
                _numberToCoord[counter] = current;
                counter++;

                for (int side = 0; side < 6; side++)
                {
                    int dirForThisSide = ClockwiseFromNE[(side + 1) % 6]; // 第一段沿 E 走，下段沿 SE...
                    int stepsThisSide = (side == 5) ? ring - 1 : ring; // 最后一段少走一步免回到起点
                    for (int s = 0; s < stepsThisSide; s++)
                    {
                        current = current.Neighbor(dirForThisSide);
                        _coordToNumber[current] = counter;
                        _numberToCoord[counter] = current;
                        counter++;
                    }
                }
            }
        }

        /// <summary>查询某格的编号；不在编号范围内返回 -1</summary>
        public int GetNumber(HexCoordinates coord) =>
            _coordToNumber.TryGetValue(coord, out var n) ? n : -1;

        /// <summary>反查编号对应的格子</summary>
        public HexCoordinates? GetCoord(int number) =>
            _numberToCoord.TryGetValue(number, out var c) ? c : (HexCoordinates?)null;

        /// <summary>取所有已编号的格子（按编号升序）</summary>
        public IEnumerable<KeyValuePair<int, HexCoordinates>> EnumerateAll()
        {
            for (int i = 0; i <= _numberToCoord.Count - 1; i++)
                if (_numberToCoord.TryGetValue(i, out var c))
                    yield return new KeyValuePair<int, HexCoordinates>(i, c);
        }
    }
}
