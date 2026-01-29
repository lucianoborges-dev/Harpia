# SLSP: Secure Light Session Protocol

**SLSP** is a lightweight messaging and presentation protocol designed for IoT (Internet of Things) devices. It operates over TCP, providing message framing, device identification, and authenticated encryption (AEAD) with minimal overhead.

Protocol Stack Placement

*   **OSI Model:** Layers 5 (Session) and 6 (Presentation).
*   **Transport:** Runs over TCP (Layer 4).


# Message Structure

1. Plaintext Header (Unencrypted)

Used for initial identification and routing before decryption.

| Field           | Size   | Description                                         |
|-----------------|--------|-----------------------------------------------------|
| **Header**      | 1 Byte | Start of Frame marker (e.g., `0x02` for STX).       |
| **Version**     | 1 Byte | Protocol version (current: `0x01`).                 |
| **IsEncrypted** | 1 Byte | Encryption flag (`0x00`: Plain, `0x01`: Encrypted). |
| **DeviceID**    | 1 Byte | Unique identifier for the source device.            |
| **Length (L)**  | 1 Byte | Payload size (0 to 255 bytes).                      |


# 2. Payload Types

## A. Plaintext Mode (`IsEncrypted = 0x00`)

| Field       | Size    | Description                            |
|-------------|---------|----------------------------------------|
| **Payload** | L Bytes | Raw application data.                  |
| **CRC8**    | 1 Byte  | Cyclic Redundancy Check for integrity. |


## B. Secure Mode (`IsEncrypted = 0x01`)

In secure mode, SLSP uses an **AEAD** (Authenticated Encryption with Associated Data) construction.

| Field          | Size     | Description                                     |
|----------------|----------|-------------------------------------------------|
| **Nonce (IV)** | 12 Bytes | Unique Initialization Vector for the cipher.    |
| **Ciphertext** | L Bytes  | Encrypted application data.                     |
| **Tag (MAC)**  | 16 Bytes | Authentication tag (e.g., Poly1305 or AES-GCM). |


# Key Features

*   **Deterministic Framing:** Uses a `Length` field to handle TCP byte-streaming, ensuring the application receives complete messages.
*   **Session Tracking:** The `DeviceID` allows the server to manage state and cryptographic contexts for multiple concurrent connections.
*   **Security:** Built-in support for encrypted payloads with integrity verification (MAC/Tag).
*   **Efficiency:** Small 5-byte common header, optimized for 8-bit and 32-bit microcontrollers.

# Implementation (SecureChannel)

In the development environment, the protocol is abstracted by the `SecureChannel` class, which handles:

1.  **Handshake:** Initial key exchange and session establishment.
2.  **Encapsulation:** Wrapping application data into the SLSP frame.
3.  **Decapsulation:** Validating tags/CRCs and stripping headers.

