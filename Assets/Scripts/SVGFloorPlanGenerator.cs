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
        // First pass: calculate bounds of all rooms
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        
        foreach (var room in rooms)
        {
            if (room.FloorAnchor == null) continue;
            var boundary = room.FloorAnchor.PlaneBoundary2D;
            if (boundary == null || boundary.Count == 0) continue;
            
            foreach (var pt in boundary)
            {
                minX = Mathf.Min(minX, pt.x);
                maxX = Mathf.Max(maxX, pt.x);
                minY = Mathf.Min(minY, pt.y);
                maxY = Mathf.Max(maxY, pt.y);
            }
        }
        
        // Add padding for dimensions (20% on each side)
        float width = maxX - minX;
        float height = maxY - minY;
        float padding = Mathf.Max(width, height) * 0.2f;
        minX -= padding;
        maxX += padding;
        minY -= padding;
        maxY += padding;
        width = maxX - minX;
        height = maxY - minY;
        
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1000\" height=\"1000\" viewBox=\"{minX:F2} {minY:F2} {width:F2} {height:F2}\">");
        sb.AppendLine($"  <title>Floor {floorLevel} Plan</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    .wall { stroke: black; stroke-width: 0.05; fill: none; }");
        sb.AppendLine("    .room { fill: #f0f0f0; stroke: black; stroke-width: 0.05; }");
        sb.AppendLine($"    .dimension {{ font-size: {(width * 0.03f):F2}px; fill: blue; }}");
        sb.AppendLine("    .dim-line { stroke: blue; stroke-width: 0.02; fill: none; }");
        sb.AppendLine("    .arrow { stroke: blue; stroke-width: 0.02; fill: none; marker-start: url(#arrowhead-start); marker-end: url(#arrowhead-end); }");
        sb.AppendLine("  </style>");
        sb.AppendLine("  <defs>");
        sb.AppendLine($"    <marker id=\"arrowhead-end\" markerWidth=\"10\" markerHeight=\"10\" refX=\"9\" refY=\"3\" orient=\"auto\" markerUnits=\"strokeWidth\">");
        sb.AppendLine("      <path d=\"M0,0 L0,6 L9,3 z\" fill=\"blue\" />");
        sb.AppendLine("    </marker>");
        sb.AppendLine($"    <marker id=\"arrowhead-start\" markerWidth=\"10\" markerHeight=\"10\" refX=\"0\" refY=\"3\" orient=\"auto\" markerUnits=\"strokeWidth\">");
        sb.AppendLine("      <path d=\"M9,0 L9,6 L0,3 z\" fill=\"blue\" />");
        sb.AppendLine("    </marker>");
        sb.AppendLine("  </defs>");

        float dimOffset = width * 0.05f; // Offset for dimension lines
        
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
                
                // Calculate perpendicular offset for dimension line (outward from room center)
                Vector2 edge = p2 - p1;
                Vector2 perpendicular = new Vector2(-edge.y, edge.x).normalized;
                
                // Determine if we need to flip the perpendicular (should point outward)
                Vector2 roomCenter = Vector2.zero;
                foreach (var pt in boundary) roomCenter += pt;
                roomCenter /= boundary.Count;
                
                Vector2 edgeMidToCenter = roomCenter - mid;
                if (Vector2.Dot(perpendicular, edgeMidToCenter) > 0)
                    perpendicular = -perpendicular;
                
                // Offset dimension line from wall
                Vector2 offset = perpendicular * dimOffset;
                Vector2 dim1 = p1 + offset;
                Vector2 dim2 = p2 + offset;
                Vector2 dimMid = mid + offset;
                
                // Draw dimension line with arrows
                sb.AppendLine($"  <line class=\"arrow\" x1=\"{dim1.x:F2}\" y1=\"{dim1.y:F2}\" x2=\"{dim2.x:F2}\" y2=\"{dim2.y:F2}\" />");
                
                // Draw connector lines from wall to dimension line
                sb.AppendLine($"  <line class=\"dim-line\" x1=\"{p1.x:F2}\" y1=\"{p1.y:F2}\" x2=\"{dim1.x:F2}\" y2=\"{dim1.y:F2}\" />");
                sb.AppendLine($"  <line class=\"dim-line\" x1=\"{p2.x:F2}\" y1=\"{p2.y:F2}\" x2=\"{dim2.x:F2}\" y2=\"{dim2.y:F2}\" />");
                
                // Calculate rotation angle for text to align with dimension line
                float angle = Mathf.Atan2(edge.y, edge.x) * Mathf.Rad2Deg;
                // Keep text readable (don't flip upside down)
                if (angle > 90) angle -= 180;
                if (angle < -90) angle += 180;
                
                sb.AppendLine($"  <text class=\"dimension\" x=\"{dimMid.x:F2}\" y=\"{dimMid.y:F2}\" text-anchor=\"middle\" transform=\"rotate({angle:F1} {dimMid.x:F2} {dimMid.y:F2})\">{length:F2}m</text>");
            }
        }

        sb.AppendLine("</svg>");
        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
    }
}
