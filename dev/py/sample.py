import sys
import json

def read_file(file_path):
    try:
        with open(file_path, 'r') as file:
             return file.read()
    except Exception as e:
        return "Error: Reading file: {e}"

print(read_file(sys.argv[1]))



