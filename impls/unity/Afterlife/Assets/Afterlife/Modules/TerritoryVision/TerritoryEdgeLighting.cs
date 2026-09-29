using System;

namespace Afterlife.TerritoryVision
{
    /// <summary>확장 영역 전체의 외곽에서 안쪽으로 측정한 거리로 조명을 만든다.</summary>
    public static class TerritoryEdgeLighting
    {
        public const float FadeWidth = 1.8f;

        public static byte[] Create(byte[] coverage, int width, int height, float pixelsPerUnit)
        {
            if (coverage == null || width <= 0 || height <= 0 || coverage.Length != width * height)
                throw new ArgumentException("Coverage dimensions must match.");
            if (pixelsPerUnit <= 0) throw new ArgumentOutOfRangeException(nameof(pixelsPerUnit));
            var squared = new double[coverage.Length];
            for (int i = 0; i < squared.Length; i++) squared[i] = coverage[i] == 0 ? 0 : 1e12;
            int length = Math.Max(width, height);
            var input = new double[length];
            var output = new double[length];
            var sites = new int[length];
            var crossings = new double[length + 1];
            // 분리 가능한 유클리드 거리 변환. 지도 전체 크기에 선형으로 비례한다.
            for (int y = 0; y < height; y++)
            {
                Array.Copy(squared, y * width, input, 0, width);
                Transform(input, output, width, sites, crossings);
                Array.Copy(output, 0, squared, y * width, width);
            }
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++) input[y] = squared[y * width + x];
                Transform(input, output, height, sites, crossings);
                for (int y = 0; y < height; y++) squared[y * width + x] = output[y];
            }
            var result = new byte[coverage.Length];
            for (int i = 0; i < result.Length; i++)
            {
                if (coverage[i] == 0) continue;
                // 픽셀 중심 사이 거리에서 반 픽셀을 빼 테두리 위치를 근사한다.
                double inward = Math.Max(0, Math.Sqrt(squared[i]) - .5) / pixelsPerUnit;
                double t = Math.Min(1, inward / FadeWidth);
                result[i] = (byte)Math.Round(255 * t * t * (3 - 2 * t));
            }
            return result;
        }

        private static void Transform(double[] input, double[] output, int count, int[] sites, double[] crossings)
        {
            int last = 0;
            sites[0] = 0;
            crossings[0] = double.NegativeInfinity;
            crossings[1] = double.PositiveInfinity;
            for (int q = 1; q < count; q++)
            {
                double crossing;
                while (true)
                {
                    int p = sites[last];
                    crossing = (input[q] + (double)q * q - input[p] - (double)p * p) / (2 * (q - p));
                    if (crossing > crossings[last]) break;
                    last--;
                }
                sites[++last] = q;
                crossings[last] = crossing;
                crossings[last + 1] = double.PositiveInfinity;
            }
            last = 0;
            for (int q = 0; q < count; q++)
            {
                while (crossings[last + 1] < q) last++;
                double delta = q - sites[last];
                output[q] = delta * delta + input[sites[last]];
            }
        }
    }
}
