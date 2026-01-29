# ALMP: Autonomous Light Messaging Protocol

**Purpose:** An application-level protocol inspired by **JAUS**, optimized for command-and-control between autonomous components.

Dynamic Packet Structure

ALMP uses a **Presence Vector** to minimize bandwidth. Fields are only included in the payload if their corresponding bit is set in the vector.

| Field        | Size    | Type   | Description                                                    |
|--------------|---------|--------|----------------------------------------------------------------|
| **Source**   | 1 Byte  | uint8  | Originator Component ID.                                       |
| **Dest**     | 1 Byte  | uint8  | Destination Component ID.                                      |
| **Func**     | 1 Byte  | uint8  | Function Code (Command/Request ID).                            |
| **Presence** | 2 Bytes | uint16 | **Presence Vector (Bitmask)**.                                 |
| **Payload**  | Var.    | Mixed  | Sequential fields (e.g., Float32, Int32) mapped by the Vector. |

Key Feature: The Presence Vector

The `Presence` field (16-bit short) allows for highly efficient messaging. For a "Position Report" command:

*   Bit 0: Latitude (Float32)
*   Bit 1: Longitude (Float32)
*   Bit 2: Altitude (Float32)

If only Latitude and Longitude are needed, the vector is set to `0x0003` (bits 0 and 1), and the payload will contain exactly 8 bytes.