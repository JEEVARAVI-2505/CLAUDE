# Optional: local AI model (Ollama)

DocuMind works without any AI (rules-only mode). A local model can help with unusual layouts. **No paid API and no internet connection is used at processing time.**

1. Install **Ollama** (free, MIT licence) from ollama.com.
2. Open a terminal and download a model once, for example:
   `ollama pull qwen2.5:7b-instruct`
   (several GB; needs about 8 GB of RAM or a GPU for comfortable speed). **Check the model's licence before commercial use** – licences differ by model and size.
3. *Settings › Local AI*: tick **Use local AI**, keep the address `http://127.0.0.1:11434`, enter the model name and press **Check connection and model**.

How it is used: the model must answer in a strict JSON format. Every value it suggests must appear word-for-word in the document, otherwise it is discarded. When the AI and the rules disagree, both values are shown and you decide. The AI gives no “confidence percentage”, because such numbers are not reliable.

If Ollama is not running or the model is missing, DocuMind tells you and continues in rules-only mode.
