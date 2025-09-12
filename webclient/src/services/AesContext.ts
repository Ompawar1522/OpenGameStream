import { ArrayBufferToBase64, Base64ToArrayBuffer } from "./BufferHelper";

export default class AesContext {
    private readonly _key: CryptoKey;

    public constructor(key: CryptoKey) {
        this._key = key;
    }

    public static async CreateFromSecretAsync(base64Key: string): Promise<AesContext> {
        const buffer = Base64ToArrayBuffer(base64Key);

        const key = await crypto.subtle.importKey(
            "raw",
            buffer,
            { name: "AES-GCM" },
            true,
            ["encrypt", "decrypt"]
        );

        return new AesContext(key);
    }

    public async DecryptBase64(inputBase64: string, ivBase64: string, tagBase64: string): Promise<string> {
        const ciphertext = Base64ToArrayBuffer(inputBase64);
        const iv = Base64ToArrayBuffer(ivBase64);
        const tag = Base64ToArrayBuffer(tagBase64);

        const combinedCiphertext = new Uint8Array(ciphertext.byteLength + tag.byteLength);
        combinedCiphertext.set(new Uint8Array(ciphertext), 0);
        combinedCiphertext.set(new Uint8Array(tag), ciphertext.byteLength);

        try {
            const decrypted = await crypto.subtle.decrypt(
                { name: "AES-GCM", iv: iv, tagLength: 128 },
                this._key,
                combinedCiphertext
            );
            return new TextDecoder().decode(decrypted);
        } catch (err) {
            console.error("Decryption failed:", err);
            throw new Error("Decryption failed or data tampered");
        }
    }

    public async EncryptBase64(input: string): Promise<EncodeBase64Result> {
        const iv = crypto.getRandomValues(new Uint8Array(12));

        const encodedData = new TextEncoder().encode(input);

        const encryptedDataWithTag = await crypto.subtle.encrypt(
            { name: "AES-GCM", iv: iv, tagLength: 128 },
            this._key,
            encodedData
        );

        const encryptedData = new Uint8Array(encryptedDataWithTag, 0, encryptedDataWithTag.byteLength - 16);
        const tag = new Uint8Array(encryptedDataWithTag, encryptedData.byteLength);

        return ({
            Iv: ArrayBufferToBase64(iv),
            Tag: ArrayBufferToBase64(tag),
            Payload: ArrayBufferToBase64(encryptedData)
        });
    }
}

export interface EncodeBase64Result {
    Payload: string;
    Iv: string;
    Tag: string;
}