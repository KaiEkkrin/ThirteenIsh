﻿namespace ThirteenIsh.Database.Entities;

/// <summary>
/// Any of my entities that has a LastEdited that I want to update when it changes
/// </summary>
internal interface IHasLastEdited
{
    DateTimeOffset LastEdited { get; set; }
}
