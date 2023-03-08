// EliteExplorationUtility - EEU - Once.cs
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

using System.Diagnostics.CodeAnalysis;

namespace EEU.Utils;

public class Once {
    protected Action init;
    private volatile bool initialized;

    protected object locker = new();

    public Once(Action doIt) {
        init = doIt;
    }

    protected Action Initializer {
        get => init;
        set {
            if (Initialized) {
                throw new AlreadyInitializedException();
            }

            init = value;
        }
    }

    public bool Initialized => initialized;

    public static Once Create(Action doIt) {
        return new Once(doIt);
    }

    public void Init() {
        if (initialized) {
            return;
        }

        lock (locker) {
            // Repeated in case we didn't grab the lock first
            if (initialized) {
                return;
            }

            init();
            initialized = true;
        }
    }

    private class AlreadyInitializedException : Exception {
        public AlreadyInitializedException() : base("Cannot change initializer after initialization is complete.") { }
    }
}

public class Once<TElem> : Once {
    private TElem? elem;

    public Once(Func<TElem> a) : base(() => { }) {
        void RealAction() {
            elem = a();
        }

        Initializer = RealAction;
    }

    [MemberNotNullWhen(true, nameof(elem))]
    public new bool Initialized => base.Initialized;

    public TElem Elem {
        get {
            Init();
            return elem;
        }
    }

    [MemberNotNull(nameof(elem))]
    public new void Init() {
        base.Init();
        Assert.NotNull(elem);
    }
}
