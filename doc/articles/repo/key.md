# Private key storage

A private key is stored as an [encrypted PKCS #8 structure](https://tools.ietf.org/html/rfc5208) in the 
[PEM format](https://en.wikipedia.org/wiki/Privacy-Enhanced_Mail). It is protected by a key generated from the 
key chain's *passPhrase* using [PBKDF2](https://en.wikipedia.org/wiki/PBKDF2).  It is managed with 
the [KeyApi](xref:Ipfs.CoreApi.IKeyApi).

![key storage](../../images/private-key.png)

## Example

A [key](xref:Ipfs.IKey) is stored as JSON.

```json
{
  "Name": "x1",
  "Id": "QmPtBXBV3xS2FR6emopGLhHNFR2nsBdwuxitLfsHSBjuYC",
  "Pem": "-----BEGIN ENCRYPTED PRIVATE KEY-----
          MIIE9jAoB43t9wa4 ... 
          -----END ENCRYPTED PRIVATE KEY-----
		  "
}
```
