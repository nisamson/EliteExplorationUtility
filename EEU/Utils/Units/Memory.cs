// EliteExplorationUtility - EEU - Memory.cs
// Copyright (C) 2023 Nick Samson
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

namespace EEU.Utils.Units;

public abstract class Memory {
    protected ulong NumBytes { get; set; }

    public class Bytes : Memory {
        public const ulong RequisiteBytes = 1;
    }

    public class KiBytes : Memory {
        public const ulong RequisiteBytes = 1024;
    }

    public class MiBytes : Memory {
        public const ulong RequisiteBytes = KiBytes.RequisiteBytes * 1024;
    }

    public class GiBytes : Memory {
        public const ulong RequisiteBytes = MiBytes.RequisiteBytes * 1024;
    }
}
