using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Generates SVG floor plans with dimensions (quotes/kóty) for each floor level.
/// </summary>
public static class SVGFloorPlanGenerator
{
    public static void GenerateFloorPlan(List<MRUKRoom> rooms, string outputPath, int floorLevel)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1000\" height=\"1000\" viewBox=\"-10 -10 20 20\">");
        sb.AppendLine($"  <title>Floor {floorLevel} Plan</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    .wall { stroke: black; stroke-width: 0.1; fill: none; }");
        sb.AppendLine("    .room { fill: #f0f0f0; stroke: black; stroke-width: 0.05; }");
        sb.AppendLine("    .dimension { font-size: 0.3px; fill: blue; }");
        sb.AppendLine("    .arrow { stroke: blue; stroke-width: 0.02; fill: none; marker-end: url(#arrowhead); }");
        sb.AppendLine("  </style>");
        sb.AppendLine("  <defs>");
        sb.AppendLine("    <marker id=\"arrowhead\" markerWidth=\"10\" markerHeight=\"10\" refX=\"5\" refY=\"5\" orient=\"auto\">");
        sb.AppendLine("      <polygon points=\"0 0, 10 5, 0 10\" fill=\"blue\" />");
        sb.AppendLine("    </marker>");
        sb.AppendLine("  </defs>");

        foreach (var room in rooms)
        {
            if (room.FloorAnchor == null) continue;

            // Get 2D boundary from floor anchor
            var boundary = room.FloorAnchor.PlaneBoundary2D;
            if (boundary == null || boundary.Count == 0) continue;

            // Draw room polygon (project to XZ plane)
            sb.Append("  <polygon class=\"room\" points=\"");
            foreach (var pt in boundary)
            {
                sb.Append($"{pt.x:F2},{pt.y:F2} ");
            }
            sb.AppendLine("\" />");

            // Draw dimensions (kóty) for each edge
            for (int i = 0; i < boundary.Count; i++)
            {
                var p1 = boundary[i];
                var p2 = boundary[(i + 1) % boundary.Count];
                float length = Vector2.Distance(p1, p2);
                Vector2 mid = (p1 + p2) * 0.5f;
                
                // Draw dimension line and text
                sb.AppendLine($"  <line class=\"arrow\" x1=\"{p1.x:F2}\" y1=\"{p1.y:F2}\" x2=\"{p2.x:F2}\" y2=\"{p2.y:F2}\" />");
                sb.AppendLine($"  <text class=\"dimension\" x=\"{mid.x:F2}\" y=\"{mid.y:F2}\" text-anchor=\"middle\">{length:F2}m</text>");
            }
        }

        sb.AppendLine("</svg>");
        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
    }
}
