/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace AwPropdump;

/// <summary>
/// Object properties read from an ActiveWorlds property dump ("propdump")
/// </summary>
public class PropdumpObject {
    public int OwnerCitizenNumber { get; set; }
    public long BuildTimestamp { get; set; }
    public int XCoord { get; set; }
    public int YCoord { get; set; }
    public int ZCoord { get; set; }
    public int? XOrient { get; set; }
    public int YOrient { get; set; }
    public int? ZOrient { get; set; }
    public int ObjectType { get; set; }
    public string? V4Data { get; set; }
    public string? Model { get; set; }
    public string? Description { get; set; }
    public string? Action { get; set; }
}
