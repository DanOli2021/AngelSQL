import pandas as pd
import numpy as np
from statsmodels.tsa.arima.model import ARIMA
from datetime import timedelta
import json
import sys

def read_file(file_path):
    try:
        with open(file_path, 'r', encoding='utf-8-sig') as file:
             return file.read()
    except Exception as e:
        return "Error: Reading file " + file_path + ": e" + e

json_data = read_file(sys.argv[1])

# Convertir sys.argv[1] a un diccionario desde JSON
data = json.loads(json_data)

# Crear DataFrame desde el diccionario
df = pd.DataFrame(data)

# Asegurarse de que la columna 'Day' esté en formato de fecha
df['Day'] = pd.to_datetime(df['Day'])

# Establecer 'Day' como índice
df.set_index('Day', inplace=True)

# Crear y ajustar el modelo ARIMA
model = ARIMA(df['Total'], order=(5, 1, 0))
model_fit = model.fit()


# Forecast para los próximos 30 días
forecast_30_days = model_fit.forecast(steps=30)

# Crear un índice de fechas para el pronóstico
forecast_dates_30 = [df.index[-1] + timedelta(days=i) for i in range(1, 31)]

# Convertir los resultados a formato JSON
forecast = [{"Day": str(date), "Total": total} for date, total in zip(forecast_dates_30, forecast_30_days)]

print(json.dumps(forecast))
