using System.Collections.Generic;

namespace ShiftScape.Core
{
    public static class LevelLibrary
    {
        public static readonly string[] Raw =
        {
@"name: First Steps
par: 1
start: 0,1
goal: 2,1
grid:
.    .    L
R    RL   .
.    .    .",

@"name: Slide Over
par: 1
start: 0,0
goal: 0,2
grid:
D    .    .
.    UD   RL
U    .    .",

@"name: Two Turns
par: 2
start: 0,0
goal: 2,0
grid:
.    D    UL
UR   RL   .
UL   UR   D",

@"name: Crossroads
par: 2
start: 0,1
goal: 2,2
grid:
U    DL   L
DL   UD   RDL
RD   UR   R",

@"name: Wider Ground
par: 2
start: 0,0
goal: 3,1
grid:
R    RDL  UD   RL
.    RL   UR   D
.    UD   RL   UL
UR   RL   .    DL",

@"name: Detour
par: 2
start: 0,0
goal: 3,0
grid:
UR   RL   DL   UL
RL   UD   DL   D
RDL  UD   UD   UD
D    UD   UR   UD",

@"name: Winding Trail
par: 3
start: 0,2
goal: 3,3
grid:
U    URD  RD   DL
UD   UD   UR   URL
UL   UR   DL   R
RD   .    URD  UD",

@"name: Switchback
par: 3
start: 0,2
goal: 3,2
grid:
UR   DL   .    UD
RD   .    UL   L
UD   UR   RL   RD
U    R    UD   DL",

@"name: Bedrock
par: 3
start: 0,3
goal: 3,0
lockedColumns: 3
grid:
URD  .    D    D
RD   RL   RL   UL
.    .    UR   DL
UL   RL   RD   R",

@"name: Anchored
par: 3
start: 0,3
goal: 3,3
lockedColumns: 0
grid:
UR   UD   RL   UR
URL  DL   U    L
DL   .    RL   RL
L    R    UL   UD",

@"name: Fault Line
par: 4
start: 0,2
goal: 3,2
lockedRows: 1
grid:
RD   DL   .    L
.    UD   D    .
RD   RD   RL   U
UR   UR   .    UL",

@"name: Tectonic
par: 4
start: 0,0
goal: 4,0
lockedRows: 1
lockedColumns: 4
grid:
UR   D    D    RL   UL
UD   .    .    UR   UD
UD   UR   RD   .    UD
UR   DL   RL   URL  UD
RL   UR   R    UR   UL",

@"name: Continental Drift
par: 5
start: 0,2
goal: 4,4
lockedRows: 0
lockedColumns: 2
grid:
.    RD   RD   UD   .
R    UL   DL   RD   RL
UL   L    UD   RL   UD
UL   L    RD   UD   .
UR   UL   UR   UDL  L",
        };

        static List<LevelData> levels;

        public static IReadOnlyList<LevelData> Levels
        {
            get
            {
                if (levels != null) return levels;
                levels = new List<LevelData>(Raw.Length);
                foreach (string text in Raw) levels.Add(LevelParser.Parse(text));
                return levels;
            }
        }
    }
}
