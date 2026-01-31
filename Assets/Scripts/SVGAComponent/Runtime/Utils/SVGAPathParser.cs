using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Bo.SVGA
{
    public class SVGAPathParser
    {
        private readonly Regex commandRegex =
            new Regex(@"([MmLlHhVvCcSsQqTtAaZz])([^MmLlHhVvCcSsQqTtAaZz]*)", RegexOptions.Compiled);
        private readonly Regex numberRegex =
            new Regex(@"[-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?", RegexOptions.Compiled);

        private Vector2 currentPoint;
        private Vector2 firstPoint;
        private Vector2 lastControlPoint;
        private List<Vector2> pathPoints;
        private Matrix4x4 transformMatrix;

        public SVGAPathParser()
        {
            currentPoint = Vector2.zero;
            firstPoint = Vector2.zero;
            lastControlPoint = Vector2.zero;
            pathPoints = new List<Vector2>();
            transformMatrix = Matrix4x4.identity;
        }

        public List<Vector2> GetPathPoints()
        {
            return pathPoints;
        }

        private Vector2 TransformPoint(Vector2 point)
        {
            // 应用变换矩阵
            Vector3 transformedPoint = transformMatrix.MultiplyPoint3x4(new Vector3(point.x, point.y, 0));
            return new Vector2(transformedPoint.x, transformedPoint.y);
        }

        public List<Vector2> ParsePath(string svgPath)
        {
            pathPoints.Clear();
            currentPoint = Vector2.zero;
            firstPoint = Vector2.zero;
            lastControlPoint = Vector2.zero;

            var matches = commandRegex.Matches(svgPath);
            foreach (Match match in matches)
            {
                string command = match.Groups[1].Value;
                string parameters = match.Groups[2].Value.Trim();
                var numberMatches = numberRegex.Matches(parameters);
                var numbersList = new List<float>(numberMatches.Count);
                foreach (Match m in numberMatches)
                {
                    numbersList.Add(float.Parse(m.Value));
                }

                var numbers = numbersList.ToArray();

                ProcessCommand(command, numbers);
            }

            return pathPoints;
        }

        private void ProcessCommand(string command, float[] numbers)
        {
            bool isRelative = char.IsLower(command[0]);
            command = command.ToUpper();

            switch (command)
            {
                case "M":
                    MoveTo(numbers, isRelative);
                    break;
                case "L":
                    LineTo(numbers, isRelative);
                    break;
                case "H":
                    HorizontalLineTo(numbers, isRelative);
                    break;
                case "V":
                    VerticalLineTo(numbers, isRelative);
                    break;
                case "C":
                    CurveTo(numbers, isRelative);
                    break;
                case "S":
                    SmoothCurveTo(numbers, isRelative);
                    break;
                case "Q":
                    QuadraticCurveTo(numbers, isRelative);
                    break;
                case "T":
                    SmoothQuadraticCurveTo(numbers, isRelative);
                    break;
                case "A":
                    ArcTo(numbers, isRelative);
                    break;
                case "Z":
                    ClosePath();
                    break;
            }
        }

        private void MoveTo(float[] numbers, bool isRelative)
        {
            if (numbers.Length < 2)
                return;

            float x = numbers[0];
            float y = numbers[1];

            if (isRelative)
            {
                x += currentPoint.x;
                y += currentPoint.y;
            }

            currentPoint = new Vector2(x, y);
            firstPoint = currentPoint;
            pathPoints.Add(TransformPoint(currentPoint));
        }

        private void LineTo(float[] numbers, bool isRelative)
        {
            if (numbers.Length < 2)
                return;

            for (int i = 0; i < numbers.Length; i += 2)
            {
                if (i + 1 >= numbers.Length)
                    break;

                float x = numbers[i];
                float y = numbers[i + 1];

                if (isRelative)
                {
                    x += currentPoint.x;
                    y += currentPoint.y;
                }

                currentPoint = new Vector2(x, y);
                pathPoints.Add(TransformPoint(currentPoint));
            }
        }

        private void HorizontalLineTo(float[] numbers, bool isRelative)
        {
            if (numbers.Length < 1)
                return;

            for (int i = 0; i < numbers.Length; i++)
            {
                float x = numbers[i];
                float y = currentPoint.y;

                if (isRelative)
                {
                    x += currentPoint.x;
                }

                currentPoint = new Vector2(x, y);
                pathPoints.Add(TransformPoint(currentPoint));
            }
        }

        private void VerticalLineTo(float[] numbers, bool isRelative)
        {
            if (numbers.Length < 1)
                return;

            for (int i = 0; i < numbers.Length; i++)
            {
                float x = currentPoint.x;
                float y = numbers[i];

                if (isRelative)
                {
                    y += currentPoint.y;
                }

                currentPoint = new Vector2(x, y);
                pathPoints.Add(TransformPoint(currentPoint));
            }
        }

        private void ClosePath()
        {
            if (pathPoints.Count > 0 && currentPoint != firstPoint)
            {
                pathPoints.Add(firstPoint);
                currentPoint = firstPoint;
            }
        }

        private void CurveTo(float[] numbers, bool isRelative)
        {
            if (numbers.Length < 6) return;

            for (int i = 0; i < numbers.Length; i += 6)
            {
                if (i + 5 >= numbers.Length) break;

                float x1 = numbers[i];
                float y1 = numbers[i + 1];
                float x2 = numbers[i + 2];
                float y2 = numbers[i + 3];
                float x = numbers[i + 4];
                float y = numbers[i + 5];

                if (isRelative)
                {
                    x1 += currentPoint.x;
                    y1 += currentPoint.y;
                    x2 += currentPoint.x;
                    y2 += currentPoint.y;
                    x += currentPoint.x;
                    y += currentPoint.y;
                }

                Vector2 p1 = new Vector2(x1, y1);
                Vector2 p2 = new Vector2(x2, y2);
                Vector2 p3 = new Vector2(x, y);

                AddCubicBezier(currentPoint, p1, p2, p3);

                currentPoint = new Vector2(x, y);
                lastControlPoint = new Vector2(x2, y2);
            }
        }

        private void AddCubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            RecursiveCubicBezier(p0, p1, p2, p3, 0f, 1f, 0, p0, p3);
        }

        private void RecursiveCubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t0, float t1, int depth, Vector2 v0, Vector2 v1)
        {
            float tMid = (t0 + t1) * 0.5f;
            Vector2 vMid = EvaluateCubicBezier(p0, p1, p2, p3, tMid);

            float dist = Vector2.Distance(v0, v1);
            float distMid = Vector2.Distance(v0, vMid) + Vector2.Distance(vMid, v1);
            float error = distMid - dist;

            // 1. 深度过大强制结束
            // 2. 误差足够小（平滑）
            // 3. 线段非常短（性能）
            if (depth >= 16 || error < 0.01f || dist < 0.01f)
            {
                pathPoints.Add(TransformPoint(v1));
            }
            else
            {
                RecursiveCubicBezier(p0, p1, p2, p3, t0, tMid, depth + 1, v0, vMid);
                RecursiveCubicBezier(p0, p1, p2, p3, tMid, t1, depth + 1, vMid, v1);
            }
        }

        private Vector2 EvaluateCubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector2 p = uuu * p0;
            p += 3f * uu * t * p1;
            p += 3f * u * tt * p2;
            p += ttt * p3;

            return p;
        }

        private void SmoothCurveTo(float[] numbers, bool isRelative)
        {
            if (numbers.Length < 4)
                return;

            for (int i = 0; i < numbers.Length; i += 4)
            {
                if (i + 3 >= numbers.Length)
                    break;

                float x2 = numbers[i];
                float y2 = numbers[i + 1];
                float x = numbers[i + 2];
                float y = numbers[i + 3];

                if (isRelative)
                {
                    x2 += currentPoint.x;
                    y2 += currentPoint.y;
                    x += currentPoint.x;
                    y += currentPoint.y;
                }

                // 计算反射的控制点
                Vector2 p1 = new Vector2(
                    2 * currentPoint.x - lastControlPoint.x,
                    2 * currentPoint.y - lastControlPoint.y
                );
                Vector2 p2 = new Vector2(x2, y2);
                Vector2 p3 = new Vector2(x, y);

                AddCubicBezier(currentPoint, p1, p2, p3);

                currentPoint = new Vector2(x, y);
                lastControlPoint = new Vector2(x2, y2);
            }
        }

        private void QuadraticCurveTo(float[] numbers, bool isRelative)
        {
            if (numbers.Length < 4) return;

            for (int i = 0; i < numbers.Length; i += 4)
            {
                if (i + 3 >= numbers.Length) break;

                float x1 = numbers[i];
                float y1 = numbers[i + 1];
                float x = numbers[i + 2];
                float y = numbers[i + 3];

                if (isRelative)
                {
                    x1 += currentPoint.x;
                    y1 += currentPoint.y;
                    x += currentPoint.x;
                    y += currentPoint.y;
                }

                Vector2 p1 = new Vector2(x1, y1);
                Vector2 p2 = new Vector2(x, y);

                AddQuadraticBezier(currentPoint, p1, p2);

                currentPoint = new Vector2(x, y);
                lastControlPoint = new Vector2(x1, y1);
            }
        }

        private void AddQuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2)
        {
            RecursiveQuadraticBezier(p0, p1, p2, 0f, 1f, 0, p0, p2);
        }

        private void RecursiveQuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t0, float t1, int depth, Vector2 v0, Vector2 v1)
        {
            float tMid = (t0 + t1) * 0.5f;
            Vector2 vMid = EvaluateQuadraticBezier(p0, p1, p2, tMid);

            float dist = Vector2.Distance(v0, v1);
            float distMid = Vector2.Distance(v0, vMid) + Vector2.Distance(vMid, v1);
            float error = distMid - dist;

            if (depth >= 16 || error < 0.01f || dist < 0.01f)
            {
                pathPoints.Add(TransformPoint(v1));
            }
            else
            {
                RecursiveQuadraticBezier(p0, p1, p2, t0, tMid, depth + 1, v0, vMid);
                RecursiveQuadraticBezier(p0, p1, p2, tMid, t1, depth + 1, vMid, v1);
            }
        }

        private Vector2 EvaluateQuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;

            Vector2 p = uu * p0;
            p += 2f * u * t * p1;
            p += tt * p2;

            return p;
        }

        private void SmoothQuadraticCurveTo(float[] numbers, bool isRelative)
        {
            if (numbers.Length < 2)
                return;

            for (int i = 0; i < numbers.Length; i += 2)
            {
                if (i + 1 >= numbers.Length)
                    break;

                float x = numbers[i];
                float y = numbers[i + 1];

                if (isRelative)
                {
                    x += currentPoint.x;
                    y += currentPoint.y;
                }

                // 计算反射的控制点
                Vector2 p1 = new Vector2(
                    2 * currentPoint.x - lastControlPoint.x,
                    2 * currentPoint.y - lastControlPoint.y
                );
                Vector2 p2 = new Vector2(x, y);

                AddQuadraticBezier(currentPoint, p1, p2);

                currentPoint = new Vector2(x, y);
                lastControlPoint = p1;
            }
        }

        private void ArcTo(float[] numbers, bool isRelative)
        {
            if (numbers.Length < 7)
                return;

            for (int i = 0; i < numbers.Length; i += 7)
            {
                if (i + 6 >= numbers.Length)
                    break;

                float rx = numbers[i];
                float ry = numbers[i + 1];
                float xAxisRotation = numbers[i + 2];
                bool largeArcFlag = numbers[i + 3] != 0;
                bool sweepFlag = numbers[i + 4] != 0;
                float x = numbers[i + 5];
                float y = numbers[i + 6];

                if (isRelative)
                {
                    x += currentPoint.x;
                    y += currentPoint.y;
                }

                // 如果起点和终点重合，则不绘制任何内容
                if (currentPoint.x == x && currentPoint.y == y)
                {
                    continue;
                }

                // 如果半径太小，则绘制直线
                if (rx < 0.001f || ry < 0.001f)
                {
                    pathPoints.Add(TransformPoint(new Vector2(x, y)));
                    currentPoint = new Vector2(x, y);
                    continue;
                }

                // 确保半径为正值
                rx = Mathf.Abs(rx);
                ry = Mathf.Abs(ry);

                // 计算椭圆弧的参数
                float angle = xAxisRotation * Mathf.Deg2Rad;
                float cosAngle = Mathf.Cos(angle);
                float sinAngle = Mathf.Sin(angle);

                // 将终点转换到原点坐标系
                float dx = (currentPoint.x - x) / 2;
                float dy = (currentPoint.y - y) / 2;
                float x1 = cosAngle * dx + sinAngle * dy;
                float y1 = -sinAngle * dx + cosAngle * dy;

                // 调整半径
                float radiiCheck = (x1 * x1) / (rx * rx) + (y1 * y1) / (ry * ry);
                if (radiiCheck > 1)
                {
                    rx *= Mathf.Sqrt(radiiCheck);
                    ry *= Mathf.Sqrt(radiiCheck);
                }

                // 计算椭圆中心
                float sign = (largeArcFlag == sweepFlag) ? -1 : 1;
                float sq = ((rx * rx * ry * ry) - (rx * rx * y1 * y1) - (ry * ry * x1 * x1)) /
                           ((rx * rx * y1 * y1) + (ry * ry * x1 * x1));
                sq = sq < 0 ? 0 : sq;
                float coef = sign * Mathf.Sqrt(sq);
                float cx1 = coef * ((rx * y1) / ry);
                float cy1 = coef * -((ry * x1) / rx);

                // 计算最终的中心点
                float cx = cosAngle * cx1 - sinAngle * cy1 + (currentPoint.x + x) / 2;
                float cy = sinAngle * cx1 + cosAngle * cy1 + (currentPoint.y + y) / 2;

                // 计算起始角和扫描角
                float startAngle = CalculateAngle(1, 0, (x1 - cx1) / rx, (y1 - cy1) / ry);
                float deltaAngle = CalculateAngle(
                    (x1 - cx1) / rx, (y1 - cy1) / ry,
                    (-x1 - cx1) / rx, (-y1 - cy1) / ry) % (2 * Mathf.PI);

                if (!sweepFlag && deltaAngle > 0)
                    deltaAngle -= 2 * Mathf.PI;
                else if (sweepFlag && deltaAngle < 0)
                    deltaAngle += 2 * Mathf.PI;

                // 生成弧线上的点
                int segments = Mathf.Max(1, Mathf.Min(300, Mathf.CeilToInt(Mathf.Abs(deltaAngle) * 150 / Mathf.PI)));
                float dt = deltaAngle / segments;
                float ct = Mathf.Cos(angle);
                float st = Mathf.Sin(angle);

                for (int j = 0; j <= segments; j++)
                {
                    float t = startAngle + j * dt;
                    float cosT = Mathf.Cos(t);
                    float sinT = Mathf.Sin(t);
                    float px = ct * rx * cosT - st * ry * sinT + cx;
                    float py = st * rx * cosT + ct * ry * sinT + cy;
                    pathPoints.Add(TransformPoint(new Vector2(px, py)));
                }

                currentPoint = new Vector2(x, y);
            }
        }

        private float CalculateAngle(float ux, float uy, float vx, float vy)
        {
            float dot = ux * vx + uy * vy;
            float len = Mathf.Sqrt(ux * ux + uy * uy) * Mathf.Sqrt(vx * vx + vy * vy);
            float angle = Mathf.Acos(Mathf.Clamp(dot / len, -1, 1));
            if (ux * vy - uy * vx < 0)
                angle = -angle;
            return angle;
        }
    }
}