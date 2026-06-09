import numpy as np
import pandas as pd
from scipy.sparse import csr_matrix
from sklearn.decomposition import TruncatedSVD
import pickle
import json

# Load training data
print("Loading training data...")
df = pd.read_csv('training_data.csv')

# Create mappings
unique_users = df['user_id'].unique()
unique_packages = df['package_id'].unique()

user_to_idx = {user: idx for idx, user in enumerate(unique_users)}
package_to_idx = {package: idx for idx, package in enumerate(unique_packages)}
idx_to_package = {idx: package for package, idx in package_to_idx.items()}

print(f"Users: {len(unique_users)}, Packages: {len(unique_packages)}")

# Create user-item matrix
n_users = len(unique_users)
n_packages = len(unique_packages)
user_item_matrix = np.zeros((n_users, n_packages))

for _, row in df.iterrows():
    user_idx = user_to_idx[row['user_id']]
    package_idx = package_to_idx[row['package_id']]
    user_item_matrix[user_idx, package_idx] = row['rating']

print(f"Matrix shape: {user_item_matrix.shape}")

# Matrix Factorization using SVD
print("\nTraining Matrix Factorization model...")

# 🔽 FIX: n_components ko safe value pe set karo
if n_users <= 1 or n_packages <= 1:
    n_components = 1
else:
    n_components = min(5, n_users - 1, n_packages - 1)

print(f"Using n_components = {n_components}")

svd = TruncatedSVD(n_components=n_components, random_state=42)
user_factors = svd.fit_transform(user_item_matrix)
package_factors = svd.components_.T

# Save model
model_data = {
    'user_factors': user_factors.tolist(),
    'package_factors': package_factors.tolist(),
    'user_to_idx': {int(k): int(v) for k, v in user_to_idx.items()},
    'idx_to_package': {int(k): int(v) for k, v in idx_to_package.items()},
    'package_to_idx': {int(k): int(v) for k, v in package_to_idx.items()}
}

with open('recommendation_model.json', 'w') as f:
    json.dump(model_data, f, indent=2)

print("Model saved to recommendation_model.json")