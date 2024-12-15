# AutoEqApi
API to cache and distribute equalization data from the AutoEQ project.

The API remains updated by retrieving data packages from this repo: https://github.com/ThePBone/AutoEqPackages

## Endpoints
Search for headphone and speaker models by name
```
GET /results/search/{query:string}
```
#### Response
| Response header  | Description                                                 |
|------------------|-------------------------------------------------------------|
| `X-Limit`          | Amount of maximum returned results                          |
| `X-Partial-Result` | Contains 1 if there are more results than `X-Limit`, else 0 |

Body
```json
[
	{
		"n": "Google Pixel Buds", (= name)
		"s": "Rtings",            (= source)
		"r": 1,                   (= rank)
		"i": 1780                 (= id)
	}
]
```
_________________
Retrieve raw GraphicEQ equalization data string by id (as `text/plain`)
```
GET /results/{id:long}
```
| Response header | Description                           |
|-----------------|---------------------------------------|
| `X-Profile-Id`    | Id of requested profile data string   |
| `X-Profile-Name`  | Device name of requested profile data string |
| `X-Profile-Source`  | Source name of requested profile data string |
| `X-Profile-Rank`  | Rank index of requested profile data string |
