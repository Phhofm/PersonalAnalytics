﻿using System;
using GoalSetting.Goals;
using Shared.Helpers;
using Shared;

namespace GoalSetting.Visualizers
{
    internal class WeekVisualizationForDailyGoal : PAVisualization
    {
        public WeekVisualizationForDailyGoal(DateTimeOffset date, GoalActivity goal) : base(date, goal) { }

        public override string GetHtml()
        {
            var html = string.Empty;

            // CSS
            html += "<style type='text/css'>";
            html += ".c3-line { stroke-width: 2px; }";
            html += ".c3-grid text, c3.grid line { fill: black; }";
            html += ".axis path, .axis line {fill: none; stroke: black; stroke-width: 1; shape-rendering: crispEdges;}";
            html += "</style>";

            //HTML
            html += "<div id='" + VisHelper.CreateChartHtmlTitle(Title) + "' style='align: center'></div>";
            html += "<p style='text-align: center; font-size: 0.66em;'>" + GoalVisHelper.getHintText(_goal, VisType.Day) + "</p>";

            return html;
        }
    }
}