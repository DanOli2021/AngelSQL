import json
from collections import defaultdict
from mlxtend.frequent_patterns import apriori, association_rules
import pandas as pd
import sys

# Cargar el JSON con las ventas
def read_file(file_path):
    try:
        with open(file_path, 'r', encoding='utf-8-sig') as file:
            return file.read()
    except Exception as e:
        return "Error: Reading file " + file_path + ": " + str(e)

json_data = read_file(sys.argv[1])

# Cargar el JSON
sales_data = json.loads(json_data)

# Agrupar las transacciones por Sale_id
transactions = defaultdict(list)
for sale in sales_data:
    transactions[sale['Sale_id']].append(sale['Description'])

# Convertir a una lista de listas (cada lista es una transacción)
transaction_list = list(transactions.values())

# Crear un DataFrame de tipo "One-Hot Encoding"
all_items = sorted(set(item for sublist in transaction_list for item in sublist))
df_transactions = pd.DataFrame([[int(item in transaction) for item in all_items] for transaction in transaction_list], columns=all_items)

# Depuración: Verifica el DataFrame antes de aplicar Apriori
print("DataFrame de transacciones:\n", df_transactions)

# Aplicar Apriori con un soporte más bajo, por ejemplo 1%
frequent_itemsets = apriori(df_transactions, min_support=0.01, use_colnames=True)

# Verificar conjuntos frecuentes
print("Conjuntos frecuentes:\n", frequent_itemsets)

# Generar reglas de asociación con una confianza mínima de 0.5
rules = association_rules(frequent_itemsets, metric="confidence", min_threshold=0.5)

# Verificar si se encontraron reglas de asociación
if not rules.empty:
    print("Reglas de asociación:\n", rules[['antecedents', 'consequents', 'support', 'confidence', 'lift']])
else:
    print("No se encontraron reglas de asociación.")