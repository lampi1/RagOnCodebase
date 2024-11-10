import os

extensions = ['.cs', '.cshtml', '.xaml', '.config']

current_dir = os.path.dirname(os.path.abspath(__file__))

root_dir = os.path.dirname(current_dir)

output_file = 'project_dataset.txt'

found_files = []

with open(output_file, 'w', encoding='utf-8') as outfile:
    for subdir, dirs, files in os.walk(root_dir):
        for file in files:
            filepath = os.path.join(subdir, file)
            file_ext = os.path.splitext(file)[1].lower()
            if file_ext in [ext.lower() for ext in extensions]:
                found_files.append(filepath)
                description = f"Questo file, {file}, contiene il codice per gestire una parte del progetto."
                outfile.write("<|beginning_of_text|>\n")
                outfile.write("User: Spiegami il modulo.\n")
                outfile.write(f"Bot: {description}\n")
                outfile.write("User: Mostrami un esempio di codice.\n")
                outfile.write("Bot:\n")
                with open(filepath, 'r', encoding='utf-8', errors='ignore') as infile:
                    outfile.write(infile.read())
                
                outfile.write("\n<|end_of_text|>\n\n")  

if not found_files:
    print("Nessun file trovato con le estensioni specificate.")
else:
    print(f"Numero totale di file trovati: {len(found_files)}")
    print(f"Il dataset è stato salvato in {output_file}.")
