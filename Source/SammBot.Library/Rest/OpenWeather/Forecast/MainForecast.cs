﻿#region License Information (GPLv3)
/*
 * Samm-Bot - A lightweight Discord.NET bot for moderation and other purposes.
 * Copyright (C) 2021-2023 Analog Feelings
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
#endregion

using System.Text.Json.Serialization;

namespace DinoBot.Library.Rest.OpenWeather.Forecast;

public class MainForecast
{
    [JsonPropertyName("temp")]
    public float Temperature { get; set; }
    [JsonPropertyName("feels_like")]
    public float FeelsLike { get; set; }
    [JsonPropertyName("pressure")]
    public float Pressure { get; set; }
    [JsonPropertyName("humidity")]
    public float Humidity { get; set; }
    [JsonPropertyName("temp_min")]
    public float MinimumTemperature { get; set; }
    [JsonPropertyName("temp_max")]
    public float MaximumTemperature { get; set; }
    [JsonPropertyName("sea_level")]
    public float SeaLevelPressure { get; set; }
    [JsonPropertyName("grnd_level")]
    public float GroundLevelPressure { get; set; }
}