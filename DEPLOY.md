# Manual de Deploy e Configuração — AggilleDFe

O manual de deploy foi separado por sistema operacional do servidor de
destino:

- **[DEPLOY_WINDOWS.md](DEPLOY_WINDOWS.md)** — deploy em servidor Windows
  (IIS + Serviço do Windows).
- **[DEPLOY_LINUX.md](DEPLOY_LINUX.md)** — deploy em servidor Linux
  (Nginx + systemd). Ver também o script `deploy.bat` na raiz do
  repositório, que automatiza a publicação via SSH para um servidor Linux.

`AggilleDFe.Maui` é o app companion e não faz parte de nenhum dos dois
guias (é publicado à parte, com `-f <framework>` explícito).
