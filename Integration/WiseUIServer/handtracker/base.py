import torch
import os
from model import get_model
from config import cfg
from utils.logger import setup_logger

class Tester:
    def __init__(self):
        log_folder = os.path.join(cfg.output_root, 'log')
        if not os.path.exists(log_folder):
            os.makedirs(log_folder)
        logfile = os.path.join(log_folder, 'eval_' + cfg.experiment_name + '.log')
        self.logger = setup_logger(output=logfile, name="Evaluation")
        self.logger.info('Start evaluation: %s' % ('eval_' + cfg.experiment_name))


    def load_model(self, model):
        self.logger.info('Loading the model from {}...'.format(cfg.checkpoint))
        checkpoint = torch.load(cfg.checkpoint)
        model.load_state_dict(checkpoint['net'])
        self.logger.info('The model is loaded successfully.')
        return model

    def _make_model(self):
        self.logger.info("Making the model...")
        model = get_model().to(cfg.device)
        model = self.load_model(model)
        model.eval()
        self.model = model
        self.logger.info("The model is made successfully.")


