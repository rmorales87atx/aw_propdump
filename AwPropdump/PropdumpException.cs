/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace AwPropdump;

/// <summary>
/// Exception thrown during propdump parsing
/// </summary>
public class PropdumpException : Exception {
    public PropdumpException()
    {
    }

    public PropdumpException(string message)
        : base(message)
    {
    }

    public PropdumpException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
