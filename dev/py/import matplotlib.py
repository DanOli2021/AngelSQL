import matplotlib.pyplot as plt
import numpy as np

x = np.arange(1, 11)
y = np.random.randint(10, size=10)

plt.plot(x, y)
plt.xlabel('X axis')
plt.ylabel('Y axis')
plt.title('Line Graph')
plt.show()

categories = ['A', 'B', 'C', 'D', 'E']
values = [5, 7, 3, 8, 6]

plt.bar(categories, values)
plt.xlabel('Categories')
plt.ylabel('Values')
plt.title('Bar Chart')
plt.show()

data = np.random.randn(1000)

plt.hist(data, bins=30)
plt.xlabel('Value')
plt.ylabel('Frequency')
plt.title('Histogram')
plt.show()

data = np.random.randn(1000)

plt.hist(data, bins=30)
plt.xlabel('Value')
plt.ylabel('Frequency')
plt.title('Histogram')
plt.show()

x = np.random.rand(50)
y = np.random.rand(50)
sizes = np.random.rand(50) * 1000
colors = np.random.rand(50)

plt.scatter(x, y, s=sizes, c=colors, alpha=0.5)
plt.xlabel('X axis')
plt.ylabel('Y axis')
plt.title('Scatter Plot')
plt.show()


